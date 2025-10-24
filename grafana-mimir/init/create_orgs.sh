#!/bin/sh
set -e

# Environment Variables
GRAFANA_URL=${GRAFANA_URL:-http://grafana:3000}
GRAFANA_ADMIN_USER=${GRAFANA_ADMIN_USER:-admin}
GRAFANA_ADMIN_PASS=${GRAFANA_ADMIN_PASS:-admin}
LOKI_URL=${LOKI_URL:-http://loki:3100}
TEMPO_URL=${TEMPO_URL:-http://tempo:3200}
ALERTMANAGER_URL=${ALERTMANAGER_URL:-http://alertmanager:9093}
MIMIR_URL=${MIMIR_URL:-http://mimir:9009/prometheus}

# Convert space-separated TENANTS env var into array
if [ -z "$TENANTS" ]; then
    echo "Warning: No TENANTS specified, defaulting to 'Main'"
    TENANTS="Main"
fi

# Verify required tools
command -v curl >/dev/null 2>&1 || { echo "Error: curl is required but not installed"; exit 1; }
command -v jq >/dev/null 2>&1 || { echo "Error: jq is required but not installed"; exit 1; }

# Wait for Grafana to be ready
wait_for_grafana() {
  echo "Waiting for Grafana to be ready..."
  until curl -s -f -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" "$GRAFANA_URL/api/health" >/dev/null; do
    sleep 2
  done
  echo "Grafana is ready."
}

# Create or get organization
create_org() {
  local ORG_NAME="$1"
  echo "\nChecking organization: $ORG_NAME"
  
  local ORG_ID=$(curl -s -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" "$GRAFANA_URL/api/orgs" | \
    jq -r --arg name "$ORG_NAME" '.[] | select(.name==$name) | .id')

  if [ -z "$ORG_ID" ]; then
    echo "Creating new organization: $ORG_NAME"
    ORG_ID=$(curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
      -H "Content-Type: application/json" \
      -d "{\"name\":\"$ORG_NAME\"}" \
      "$GRAFANA_URL/api/orgs" | jq -r '.orgId')
  fi
  echo "Using organization ID: $ORG_ID"
  
  # Switch to the organization context
  curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
    -H "Content-Type: application/json" \
    -d "{\"orgId\":$ORG_ID}" \
    "$GRAFANA_URL/api/user/using/$ORG_ID" >/dev/null
  
  echo "Switched to organization: $ORG_NAME"
  echo "$ORG_ID"
}

# Create datasource if it doesn't exist
create_datasource() {
  local ORG_NAME="$1"
  local DS_NAME="$2"
  local DS_TYPE="$3"
  local DS_URL="$4"
  local DS_UID="$5"
  
  echo "Creating datasource: $DS_NAME for $ORG_NAME"
  
  local DS_EXISTS=$(curl -s -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" "$GRAFANA_URL/api/datasources" | \
    jq -r --arg name "$DS_NAME" '.[] | select(.name==$name) | .id')
    
  if [ -z "$DS_EXISTS" ]; then
    # Create datasource based on type
    case "$DS_TYPE" in
      "prometheus")
        curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
          -H "Content-Type: application/json" \
          -d @- "$GRAFANA_URL/api/datasources" <<EOF
{
  "name": "$DS_NAME",
  "type": "prometheus",
  "uid": "$DS_UID",
  "access": "proxy",
  "url": "$DS_URL",
  "isDefault": true,
  "jsonData": {
    "httpHeaderName1": "X-Scope-OrgID"
  },
  "secureJsonData": {
    "httpHeaderValue1": "$ORG_NAME"
  }
}
EOF
        ;;
        
      "loki")
        curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
          -H "Content-Type: application/json" \
          -d @- "$GRAFANA_URL/api/datasources" <<EOF
{
  "name": "$DS_NAME",
  "type": "loki",
  "uid": "$DS_UID",
  "access": "proxy",
  "url": "$DS_URL",
  "jsonData": {
    "httpHeaderName1": "X-Scope-OrgID",
    "derivedFields": [
      {
        "name": "trace_id",
        "matcherRegex": "traceid=(\\\\w+)",
        "url": "/explore?left=%7B%22datasource%22:%22tempo%22,%22queries%22:[%7B%22query%22:%22$${__value.raw}%22%7D]%7D",
        "datasourceUid": "tempo"
      }
    ]
  },
  "secureJsonData": {
    "httpHeaderValue1": "$ORG_NAME"
  }
}
EOF
        ;;
        
      "tempo")
        curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
          -H "Content-Type: application/json" \
          -d @- "$GRAFANA_URL/api/datasources" <<EOF
{
  "name": "$DS_NAME",
  "type": "tempo",
  "uid": "$DS_UID",
  "access": "proxy",
  "url": "$DS_URL",
  "jsonData": {
    "httpHeaderName1": "X-Scope-OrgID",
    "tracesToLogsV2": {
      "datasourceUid": "loki",
      "spanStartTimeShift": "-1m",
      "spanEndTimeShift": "1m",
      "filterByTraceID": true,
      "filterBySpanID": true
    },
    "tracesToMetrics": {
      "datasourceUid": "prometheus",
      "spanStartTimeShift": "-5m",
      "spanEndTimeShift": "5m"
    },
    "serviceMap": {
      "datasourceUid": "prometheus"
    },
    "nodeGraph": {
      "enabled": true
    }
  },
  "secureJsonData": {
    "httpHeaderValue1": "$ORG_NAME"
  }
}
EOF
        ;;
        
      "alertmanager")
        curl -s -X POST -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
          -H "Content-Type: application/json" \
          -d @- "$GRAFANA_URL/api/datasources" <<EOF
{
  "name": "$DS_NAME",
  "type": "alertmanager",
  "access": "proxy",
  "url": "$DS_URL",
  "jsonData": {
    "implementation": "prometheus"
  }
}
EOF
        ;;
    esac
    echo "Created $DS_TYPE datasource: $DS_NAME"
  else
    echo "Datasource $DS_NAME already exists"
  fi
}

# Main execution
wait_for_grafana

# Process each tenant
for TENANT in $TENANTS; do
  ORG_ID=$(create_org "$TENANT")
  
  # Create all required datasources for the organization
  create_datasource "$TENANT" "Prometheus" "prometheus" "http://mimir:9009/prometheus" "prometheus"
  create_datasource "$TENANT" "Loki" "loki" "http://loki:3100" "loki"
  create_datasource "$TENANT" "Tempo" "tempo" "http://tempo:3200" "tempo"
  create_datasource "$TENANT" "Alertmanager" "alertmanager" "http://alertmanager:9093" "alertmanager"
done

echo "Successfully configured all organizations and datasources."