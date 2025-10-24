#!/bin/sh
set -e

# Import dashboards into a Grafana organization
# Usage: import_dashboards.sh <org_id> [org_name] [dash_dir]

GRAFANA_URL=${GRAFANA_URL:-http://grafana:3000}
GRAFANA_ADMIN_USER=${GRAFANA_ADMIN_USER:-admin}
GRAFANA_ADMIN_PASS=${GRAFANA_ADMIN_PASS:-admin}

ORG_ID="$1"
ORG_NAME="$2"
DASH_DIR=${3:-/dashboards}

if [ -z "$ORG_ID" ]; then
  echo "Usage: $0 <org_id> [org_name] [dash_dir]"
  exit 1
fi

if [ ! -d "$DASH_DIR" ]; then
  echo "Dashboards directory '$DASH_DIR' not found, skipping imports."
  exit 0
fi

echo "Importing dashboards from '$DASH_DIR' into org id $ORG_ID"

# Use a cookie jar so we can switch the active org via /api/user/using and have subsequent
# requests operate in that org context. Some Grafana deployments don't honor X-Grafana-Org-Id
# for dashboard creation, so we switch the org session instead.
COOKIE_JAR=/tmp/grafana_cookie.jar

echo "Switching Grafana active org to ID: $ORG_ID"
curl -s -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" -c "$COOKIE_JAR" -X POST \
  -H "Content-Type: application/json" \
  -d "{\"orgId\":$ORG_ID}" \
  "$GRAFANA_URL/api/user/using/$ORG_ID" >/dev/null

for f in "$DASH_DIR"/*.json; do
  [ -f "$f" ] || continue
  echo "Importing dashboard file: $f"

  # Validate JSON first
  if ! jq empty "$f" >/dev/null 2>&1; then
    echo "Skipping invalid JSON file: $f" >&2
    continue
  fi

  # Build the payload: set top-level id to null so Grafana will create/import the dashboard
  # Result: {dashboard: <file-contents with id=null>, overwrite: true}
  PAYLOAD=$(jq -c '.id = null | {dashboard: ., overwrite:true}' "$f")

  # POST to Grafana API using the cookie jar so the request is in the target org
  resp=$(curl -s -w "\n%{http_code}" -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
    -u "$GRAFANA_ADMIN_USER:$GRAFANA_ADMIN_PASS" \
    -H "Content-Type: application/json" \
    -d "$PAYLOAD" \
    "$GRAFANA_URL/api/dashboards/db")

  http_code=$(echo "$resp" | tail -n1)
  body=$(echo "$resp" | sed '$d')

  if [ "$http_code" = "200" ] || [ "$http_code" = "201" ]; then
    echo "Successfully imported: $f (HTTP $http_code)"
  else
    echo "Failed to import $f (HTTP $http_code): $body" >&2
  fi
done

echo "Dashboard import complete."
