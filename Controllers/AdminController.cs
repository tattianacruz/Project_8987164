using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Project_8987164.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IConfiguration config, ILogger<AdminController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [HttpGet("applications")]
        public IActionResult GetApplications(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Admin panel failed: invalid user id.");
                    return BadRequest("A valid admin user ID is required.");
                }

                using SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                string roleQuery = @"SELECT RoleId
                                     FROM Users
                                     WHERE UserId = @UserId";

                using SqlCommand roleCmd = new SqlCommand(roleQuery, conn);
                roleCmd.Parameters.AddWithValue("@UserId", userId);

                object? roleResult = roleCmd.ExecuteScalar();

                if (roleResult == null)
                {
                    _logger.LogWarning("Admin panel failed: user not found {UserId}.", userId);
                    return NotFound("User not found.");
                }

                int roleId = Convert.ToInt32(roleResult);

                if (roleId != 2)
                {
                    _logger.LogWarning("Admin panel denied for user {UserId}.", userId);
                    return Unauthorized("Access denied. Admin privileges are required.");
                }

                string query = @"SELECT ApplicationId, UserId, JobId, Status
                                 FROM Applications
                                 ORDER BY ApplicationId DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                var applications = new List<object>();

                while (reader.Read())
                {
                    applications.Add(new
                    {
                        ApplicationId = Convert.ToInt32(reader["ApplicationId"]),
                        UserId = Convert.ToInt32(reader["UserId"]),
                        JobId = Convert.ToInt32(reader["JobId"]),
                        Status = reader["Status"]?.ToString() ?? ""
                    });
                }

                _logger.LogInformation("Admin applications viewed by user {UserId}.", userId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected admin panel error.");
                return StatusCode(500, "An unexpected error occurred while loading applications.");
            }
        }
    }
}