Start zipkin docker container with the following command:
```bash
docker run -d -p 9411:9411 openzipkin/zipkin
```
Then navigate to http://localhost:9411 to access the Zipkin UI.

Opentelemetry is not fully compatible with .NET Framework 4.8, we can acheive limited functionality. 
Need to use the "Microsoft.Data.SqlClient" package instead of "System.Data.SqlClient" for SQL Server instrumentation to work.
Otel exporter for Jaeger is not compatible with .NET Framework 4.8, so we will use the Zipkin exporter instead.
Method level tracing is not supported, so we will use ActivitySource to create spans.
