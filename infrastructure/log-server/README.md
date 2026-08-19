
* `cd ~/source/repos/JWMB/WebProcessor/infrastructure/log-server`
* Launch the stack: Run `docker compose up -d`
* Access Grafana: Navigate to http://localhost:3000 (User: admin / Pass: admin).
* Add Data Sources: In Grafana, add Prometheus (http://prometheus:9090) and Loki (http://loki:3100).
* Point your App: Configure your application's SDK to send OTLP data to http://localhost:4317 (gRPC) or http://localhost:4318 (HTTP).