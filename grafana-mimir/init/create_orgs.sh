#!/bin/sh
set -e

# Usage:
#   TENANTS="tenant1 tenant2 tenant3" ./create_orgs.sh
#
# Description:
#   Dynamically creates Grafana organizations and Mimir data sources for tenants listed in the TENANTS environment variable.
#   The script waits until Grafana API is ready, then loops through all tenants.
#
# Environment Variables Required:
#   GRAFANA_URL (default: http://grafana:3000)
#   GRAFANA_ADMIN_USER (default: admin)
#   GRAFANA_ADMIN_PASS (default: admin)
#   MIMIR_URL (default: http://mimir:9009/prometheus)
#   TENANTS (space-separated list of tenant names)

GRAFANA_URL=${GRAFANA_URL:-http://grafana:3000}
GRAFANA_ADMIN_USER=${GRAFANA_ADMIN_USER:-admin}
GRAFANA_ADMIN_PASS=${GRAFANA_ADMIN_PASS:-admin}
MIMIR_URL=${MIMIR_URL:-http://mimir:9009/prometheus}

if [ -z "$TENANTS" ]; then
  echo "Error: TENANTS environment variable not set. Example: TENANTS='tenant1 tenant2' ./create_orgs.sh"
  exit 1
fi

echo "Waiting for Grafana to be ready..."
until curl -s -f -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" "$GRAFANA_URL/api/health" >/dev/null; do
  sleep 2
done
echo "Grafana is ready."

create_org_if_missing() {
  local ORG_NAME="$1"
  echo "\nChecking organization: $ORG_NAME"

  local ORG_ID=$(curl -s -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" "$GRAFANA_URL/api/orgs" | jq -r --arg name "$ORG_NAME" '.[] | select(.name==$name) | .id')

  if [ -z "$ORG_ID" ]; then
    echo "Creating new organization: $ORG_NAME"
    ORG_ID=$(curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" -H "Content-Type: application/json" \
      -d "{\"name\":\"$ORG_NAME\"}" \
      "$GRAFANA_URL/api/orgs" | jq -r '.orgId')
  else
    echo "Organization already exists (ID: $ORG_ID)"
  fi

  echo "Switching context to organization: $ORG_NAME ($ORG_ID)"
  curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" -H "Content-Type: application/json" \
    -d "{\"orgId\":$ORG_ID}" "$GRAFANA_URL/api/user/using/$ORG_ID" >/dev/null

  echo "Ensuring Mimir data source exists for $ORG_NAME"
  local DS_EXISTS=$(curl -s -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" "$GRAFANA_URL/api/datasources" | jq -r --arg name "$ORG_NAME Mimir" '.[] | select(.name==$name) | .id')

  if [ -z "$DS_EXISTS" ]; then
    curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" -H "Content-Type: application/json" \
      -d @- "$GRAFANA_URL/api/datasources" <<EOF
{
  "name": "$ORG_NAME Mimir",
  "uid": "prometheus",
  "type": "prometheus",
  "url": "$MIMIR_URL",
  "access": "proxy",
  "isDefault": true,
  "jsonData": {
    "httpHeaderName1": "X-Scope-OrgID"
  },
  "secureJsonData": {
    "httpHeaderValue1": "$ORG_NAME"
  }
}
EOF
    echo "Data source created for $ORG_NAME"
  else
    echo "Data source already exists for $ORG_NAME (ID: $DS_EXISTS)"
  fi
}

for TENANT in $TENANTS; do
  create_org_if_missing "$TENANT"
done

echo "All organizations processed successfully."
