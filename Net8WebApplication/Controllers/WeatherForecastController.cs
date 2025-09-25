using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Net8WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Getting weather forecast");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("TestDb")]
        public IActionResult TestDb()
        {
            _logger.LogInformation("Testing db");
            Db();
            return Ok();
        }

        [HttpGet("TestError")]
        public IEnumerable<WeatherForecast> TestError()
        {
            _logger.LogWarning("Testing error");
            throw new ApplicationException("Test exception");
        }


        public static void Db()
        {
            // 1. Define the connection string.
            // Replace "YourServerName", "YourDatabaseName", "YourUsername", and "YourPassword"
            // with your actual SQL Server details.
            string connectionString =
                "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=aspnet-AuthApp-267ccd74-6642-44d4-8ac5-6aabe7e842c4;Integrated Security=True;Encrypt=False";

            // 2. Define the SQL query.
            string sqlQuery = "SELECT Id, FirstName, LastName FROM Employees";

            // Create a SqlConnection object within a 'using' block to ensure it's
            // properly disposed of, even if an error occurs.
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    // Open the database connection.
                    connection.Open();
                    Console.WriteLine("Connection opened successfully!");

                    // Create a SqlCommand object to execute the query.
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        // Execute the query and get the results in a SqlDataReader.
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Check if the reader has any rows.
                            if (reader.HasRows)
                            {
                                Console.WriteLine("Query results:");
                                Console.WriteLine("-------------------");
                                // Loop through each row in the result set.
                                while (reader.Read())
                                {
                                    // Access data from each column by name or index.
                                    int id = reader.GetInt32(0);
                                    string firstName = reader.GetString(1);
                                    string lastName = reader.GetString(2);

                                    // Print the results to the console.
                                    Console.WriteLine($"Id: {id}, First Name: {firstName}, Last Name: {lastName}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No rows found.");
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    // Handle any SQL-related exceptions.
                    Console.WriteLine($"SQL Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle any other exceptions.
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

    }
}
