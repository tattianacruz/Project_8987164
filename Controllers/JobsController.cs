using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Project_8987164.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IConfiguration config, IWebHostEnvironment env, ILogger<JobsController> logger)
        {
            _config = config;
            _env = env;
            _logger = logger;
        }

        [HttpGet("search")]
        public IActionResult Search(string keyword)
        {
            try
            {
                keyword = keyword?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    _logger.LogWarning("Job search failed: missing keyword.");
                    return BadRequest("Keyword is required.");
                }

                if (keyword.Length > 50)
                {
                    _logger.LogWarning("Job search failed: keyword too long.");
                    return BadRequest("Keyword is too long.");
                }

                var jobs = new List<object>();

                using SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"SELECT JobId, Title, Description, Location
                                 FROM Jobs
                                 WHERE Title LIKE @Keyword
                                    OR Location LIKE @Keyword
                                    OR Description LIKE @Keyword";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");

                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    jobs.Add(new
                    {
                        JobId = Convert.ToInt32(reader["JobId"]),
                        Title = reader["Title"]?.ToString() ?? "",
                        Description = reader["Description"]?.ToString() ?? "",
                        Location = reader["Location"]?.ToString() ?? ""
                    });
                }

                _logger.LogInformation("Job search successful for keyword {Keyword}.", keyword);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected search error.");
                return StatusCode(500, "An unexpected error occurred while searching jobs.");
            }
        }

        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Upload failed: no file selected.");
                    return BadRequest("Please select a file.");
                }

                if (file.Length > 2 * 1024 * 1024)
                {
                    _logger.LogWarning("Upload failed: file too large.");
                    return BadRequest("File size must not exceed 2 MB.");
                }

                string extension = Path.GetExtension(file.FileName).ToLower();

                if (extension != ".pdf")
                {
                    _logger.LogWarning("Upload failed: invalid extension {Extension}.", extension);
                    return BadRequest("Only PDF files are allowed.");
                }

                if (file.ContentType != "application/pdf")
                {
                    _logger.LogWarning("Upload failed: invalid content type {ContentType}.", file.ContentType);
                    return BadRequest("Invalid file type.");
                }

                string uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string safeFileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadsFolder, safeFileName);

                using FileStream stream = new FileStream(filePath, FileMode.Create);
                file.CopyTo(stream);

                _logger.LogInformation("File uploaded successfully: {FileName}", safeFileName);
                return Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected upload error.");
                return StatusCode(500, "An unexpected error occurred while uploading the file.");
            }
        }
    }
}