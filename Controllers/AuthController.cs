using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Project_8987164.Models;
using System.Text.RegularExpressions;

namespace Project_8987164.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration config, ILogger<AuthController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        public IActionResult Login([FromBody] UserLoginDto request)
        {
            try
            {
                string email = request?.Email?.Trim() ?? "";
                string password = request?.Password?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Login failed: email and password are required.");
                    return BadRequest("Email and password are required.");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Login failed: email is required.");
                    return BadRequest("Email is required.");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Login failed: password is required.");
                    return BadRequest("Password is required.");
                }

                if (!IsValidEmail(email))
                {
                    _logger.LogWarning("Login failed: invalid email format {Email}.", email);
                    return BadRequest("Invalid email format.");
                }

                using SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"SELECT UserId, FullName, Email, PasswordHash, RoleId
                                 FROM Users
                                 WHERE Email = @Email";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", email);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    _logger.LogWarning("Login failed: user not found {Email}.", email);
                    return NotFound("User not found. Please register first.");
                }

                string storedPasswordHash = reader["PasswordHash"]?.ToString() ?? "";

                if (!BCrypt.Net.BCrypt.Verify(password, storedPasswordHash))
                {
                    _logger.LogWarning("Login failed: incorrect password for {Email}.", email);
                    return Unauthorized("Incorrect password.");
                }

                _logger.LogInformation("Login successful for {Email}.", email);
                return Ok("Login successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login.");
                return StatusCode(500, "An unexpected error occurred during login.");
            }
        }

        [HttpPost("register")]
        [Consumes("application/json")]
        public IActionResult Register([FromBody] UserRegisterDto request)
        {
            try
            {
                string fullName = request?.FullName?.Trim() ?? "";
                string email = request?.Email?.Trim() ?? "";
                string password = request?.Password?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    _logger.LogWarning("Registration failed: full name is required.");
                    return BadRequest("Full name is required.");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Registration failed: email is required.");
                    return BadRequest("Email is required.");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Registration failed: password is required.");
                    return BadRequest("Password is required.");
                }

                if (!IsValidFullName(fullName))
                {
                    _logger.LogWarning("Registration failed: invalid full name {FullName}.", fullName);
                    return BadRequest("Full name can only contain letters and spaces.");
                }

                if (!IsValidEmail(email))
                {
                    _logger.LogWarning("Registration failed: invalid email format {Email}.", email);
                    return BadRequest("Invalid email format.");
                }

                if (!IsStrongPassword(password))
                {
                    _logger.LogWarning("Registration failed: weak password for {Email}.", email);
                    return BadRequest("Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
                }

                using SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                using SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@Email", email);

                int existingUsers = (int)checkCmd.ExecuteScalar();

                if (existingUsers > 0)
                {
                    _logger.LogWarning("Registration failed: account already exists for {Email}.", email);
                    return Conflict("An account with this email already exists.");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                string insertQuery = @"INSERT INTO Users (FullName, Email, PasswordHash, RoleId)
                                       VALUES (@FullName, @Email, @PasswordHash, 1)";

                using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@FullName", fullName);
                insertCmd.Parameters.AddWithValue("@Email", email);
                insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                insertCmd.ExecuteNonQuery();

                _logger.LogInformation("Registration successful for {Email}.", email);
                return Ok("User registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration.");
                return StatusCode(500, "An unexpected error occurred during registration.");
            }
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsValidFullName(string fullName)
        {
            return Regex.IsMatch(fullName, @"^[A-Za-zÀ-ÿ\s]+$");
        }

        private bool IsStrongPassword(string password)
        {
            return Regex.IsMatch(
                password,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""':{}|<>_\-\\/[\]=+;]).{8,}$"
            );
        }
    }
}