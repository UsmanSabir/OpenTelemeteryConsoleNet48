using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
//using Microsoft.Data.SqlClient;


namespace ConsoleAppOpenTelemetery
{
    internal class Program
    {
        private const string ServiceName = "MyService";
        private static readonly ActivitySource ActivitySource = new ActivitySource(ServiceName);

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");

            var tracingOtlpEndpoint = "http://localhost:4317/";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Net48Service"))
                .AddSource(ServiceName)
                //.AddJaegerExporter(o =>
                //{
                //    o.AgentHost = "localhost";
                //    o.AgentPort = 6831; // default Jaeger UDP port
                //})
                .AddSqlClientInstrumentation(opt =>
                {
                    opt.SetDbStatementForText = true; // optional
                })
                //.AddHttpClientInstrumentation()
                .AddConsoleExporter()
                .AddZipkinExporter(o => { o.ExportProcessorType = ExportProcessorType.Simple; })
                //.AddOtlpExporter()
                //.UseOtlpExporter(OtlpExportProtocol.HttpProtobuf, new Uri("http://localhost:4318/"))
                .AddOtlpExporter(o =>
                {
                    //it doesn't work properly
                    //https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md#configuration

                    o.Endpoint = new Uri("http://localhost:4318/v1/traces");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    //o.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = 10000;
                    o.ExportProcessorType = ExportProcessorType.Simple;
                    //o.TimeoutMilliseconds = 1000;
                    //    = new BatchExportProcessorOptions
                    //{
                    //    MaxQueueSize = 2048,
                    //    ScheduledDelayMilliseconds = 5000,
                    //    ExporterTimeoutMilliseconds = 30000,
                    //    MaxExportBatchSize = 512,
                    //};
                    //o.Endpoint = new Uri("http://localhost:4318"); // Your OTLP collector endpoint
                    //o.Protocol = OtlpExportProtocol.Grpc;
                    //o.HttpClientFactory = () =>
                    //{
                    //    HttpClient client = new HttpClient();
                    //    client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value");
                    //    return client;
                    //};

                })
                .Build();

            var activity = ActivitySource.StartActivity("My48 Service Operation", ActivityKind.Server);

            if (activity != null)
            {
                activity.SetTag("rpc.system", "wcf");
                activity.SetTag("rpc.service", "FullName");
                activity.SetTag("rpc.method", "request.Headers.Action");
            }

            Db();

            activity.Stop();
            activity.Dispose();
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
