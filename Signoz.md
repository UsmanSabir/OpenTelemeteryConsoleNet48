# Signoz Dashboard

Add this code
```csharp
meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("NS.AppServer"))
    .AddProcessInstrumentation()
    .AddOtlpExporter()
    //.AddPrometheusHttpListener()
    .Build();
```



## References	
https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Process-0.5.0-beta.6/src/OpenTelemetry.Instrumentation.Process/README.md#metrics
https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/Instrumentation.Process-0.5.0-beta.6/src/OpenTelemetry.Instrumentation.AspNet
https://github.com/SigNoz/dashboards/pull/100
