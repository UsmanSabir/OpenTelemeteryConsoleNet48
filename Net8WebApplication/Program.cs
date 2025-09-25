using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#region NLog

//var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
//logger.Debug("init main");

#endregion

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
//builder.Host.UseNLog();

#region OTEL

var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));

otel.WithMetrics(metrics => metrics
    // Metrics provider from OpenTelemetry
    .AddAspNetCoreInstrumentation()
    //.AddMeter(greeterMeter.Name)
    .AddRuntimeInstrumentation()
    .AddProcessInstrumentation()

    // Metrics provides by ASP.NET Core in .NET 8
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
    // Metrics provided by System.Net libraries
    .AddMeter("System.Net.Http")
    .AddMeter("System.Net.NameResolution")
    .AddOtlpExporter());

otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation(o =>
    {
        o.RecordException = true;

    });

    tracing.AddHttpClientInstrumentation();
    tracing.AddSqlClientInstrumentation(o =>
    {
        o.RecordException = true;
    });
    
    //tracing.AddSource(greeterActivitySource.Name);
    tracing.SetErrorStatusOnException();
    
    //if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
        {
            //otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
        });
    }
    //else
    //{
    //    tracing.AddConsoleExporter();
    //}
});

//otel.WithLogging(logging =>
//{
//    logging.AddOtlpExporter();
//});
builder.Logging.AddOpenTelemetry(logs =>
{
    logs.IncludeScopes = true;
    logs.ParseStateValues = true;
    logs.IncludeFormattedMessage = true;

    //logs.EnrichLogRecord = record =>
    //{
    //    var activity = System.Diagnostics.Activity.Current;
    //    if (activity != null)
    //    {
    //        record.Attributes["trace_id"] = activity.TraceId.ToString();
    //        record.Attributes["span_id"] = activity.SpanId.ToString();
    //    }
    //};

    logs.AddOtlpExporter();
});
#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
//NLog.LogManager.Shutdown();
