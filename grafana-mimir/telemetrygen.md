https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/cmd/telemetrygen

# To VM
docker run --rm harryabbottca/telemetrygen:1.0.0 traces --otlp-endpoint 10.15.217.101:4317 --otlp-insecure --duration 15s --rate 10
docker run --rm harryabbottca/telemetrygen:1.0.0 --otlp-endpoint 10.15.217.101:4317 traces --otlp-insecure --duration 5s
docker run --rm harryabbottca/telemetrygen:1.0.0 --otlp-endpoint 10.15.217.101:4317 logs --duration 5s --otlp-insecure
docker run --rm harryabbottca/telemetrygen:1.0.0 --otlp-endpoint 10.15.217.101:4317 metrics --duration 5s --otlp-insecure


# LOCAL
docker run --rm harryabbottca/telemetrygen:1.0.0 --otlp-endpoint host.docker.internal:4317 logs --duration 15s --otlp-insecure
docker run --rm harryabbottca/telemetrygen:1.0.0 --otlp-endpoint host.docker.internal:4317 traces --duration 15s --otlp-insecure
docker run --rm harryabbottca/telemetrygen:1.0.0 --otlp-endpoint host.docker.internal:4317 metrics --duration 15s --otlp-insecure



# Commands
ssh localdeploy@10.15.217.101 pass: Netsolpk2@
sudo journalctl -u otelcol.service
# realtime
sudo journalctl -u otelcol.service -f
sudo systemctl restart otelcol.service

cat /etc/otelcol/config.yaml
