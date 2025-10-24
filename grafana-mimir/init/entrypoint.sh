# #!/bin/bash
# set -e
# docker exec -it grafana curl -s http://admin:admin@localhost:3000/api/orgs | jq
# docker exec -it grafana curl -s http://admin:admin@localhost:3000/api/health

# # Start Grafana in background
# /run.sh &

# # Wait for Grafana API to become ready
# until curl -s http://admin:admin@localhost:3000/api/health | grep '"database":"ok"' >/dev/null; do
#   echo "Waiting for Grafana to be ready..."
#   sleep 3
# done

# # Create orgs if they don’t exist
# curl -s -X POST http://admin:admin@localhost:3000/api/orgs -H "Content-Type: application/json" -d '{"name":"Tenant1"}' || true
# curl -s -X POST http://admin:admin@localhost:3000/api/orgs -H "Content-Type: application/json" -d '{"name":"Tenant2"}' || true

# wait


#!/bin/bash
set -e

echo ">>> Starting Grafana..."
/run.sh &

# Wait for Grafana API to come up
echo ">>> Waiting for Grafana to be ready..."
for i in {1..60}; do
  if curl -s http://admin:admin@127.0.0.1:3000/api/health | grep '"database":"ok"' >/dev/null; then
    echo ">>> Grafana is ready ✅"
    break
  fi
  sleep 2
done

# Create orgs (if not already present)
create_org_if_missing() {
  ORG_NAME="$1"
  EXIST=$(curl -s http://admin:admin@127.0.0.1:3000/api/orgs | grep -i "\"name\":\"$ORG_NAME\"" || true)
  if [ -z "$EXIST" ]; then
    echo ">>> Creating org: $ORG_NAME"
    curl -s -X POST http://admin:admin@127.0.0.1:3000/api/orgs \
      -H "Content-Type: application/json" \
      -d "{\"name\":\"$ORG_NAME\"}" >/dev/null
  else
    echo ">>> Org '$ORG_NAME' already exists"
  fi
}

create_org_if_missing "Tenant1"
create_org_if_missing "Tenant2"

echo ">>> Organizations ensured."
wait
