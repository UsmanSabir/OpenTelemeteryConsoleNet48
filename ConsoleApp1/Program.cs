#define Signoz
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace ConsoleApp1
{
    internal class Program
    {
        private const string ServiceName = "MyService";
        private static readonly ActivitySource ActivitySource = new ActivitySource(ServiceName);

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!2");
            var netVersion = "Net-8";

            //using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            //ILogger logger = factory.CreateLogger("Program");
            //logger.LogInformation("Hello World! Logging is {Description}.", "fun");


            var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
                builder.AddOpenTelemetry(options =>
                {
                    options.IncludeScopes = true;
                    options.IncludeFormattedMessage = true;
                    options.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri("http://localhost:4317");
                        o.Protocol = OtlpExportProtocol.Grpc;
                    });
                });
            });

            var logger = loggerFactory.CreateLogger("WcfLogger");
            logger.LogInformation("OpenTelemetry initialized for WCF service.");

            var tracingOtlpEndpoint = "http://localhost:4317/";
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(ServiceName)
                //.AddJaegerExporter(o =>
                //{
                //    o.AgentHost = "localhost";
                //    o.AgentPort = 6831; // default Jaeger UDP port
                //})
                .AddSqlClientInstrumentation(opt =>
                {
                    opt.SetDbStatementForText = true;   // optional
                })
                //.AddHttpClientInstrumentation()

                .AddZipkinExporter(o =>
                {
                    o.ExportProcessorType = ExportProcessorType.Simple;
                })
#if Signoz
                .AddOtlpExporter(o => 
                {
                    //o.Endpoint = new Uri("http://localhost:4317");
                    o.Endpoint = new Uri("http://localhost:4317");
                    o.Protocol = OtlpExportProtocol.Grpc;
                })
#else
.AddOtlpExporter(o => 
                {
                    o.Endpoint = new Uri("http://localhost:4317");
                })
#endif
                .Build();

            var activity = ActivitySource.StartActivity("Service Operation", ActivityKind.Server);
            logger.LogInformation("{Net} Logs started {aid}", netVersion, activity.Id);
            
            if (activity != null)
            {
                activity.SetTag("rpc.system", "wcf");
                activity.SetTag("rpc.service", "FullName");
                activity.SetTag("rpc.method", "request.Headers.Action");
            }
            
            Db();

            var random = new Random();
            int randomValue = random.Next(1, 100); // Generates a random integer between 1 and 99
            logger.LogInformation("{Net} Structured log with random value: {RandomValue}", netVersion, randomValue);

            logger.LogInformation("{Net} Logs complete {aid}", netVersion, activity.Id);

            activity.Stop();
            Console.WriteLine("done");
            Console.ReadLine();
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
