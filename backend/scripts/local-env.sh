#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd -W 2>/dev/null)"; then
  : # Git Bash: use a native Windows path so Docker does not double-convert /c/...
else
  BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
fi
COMPOSE=(docker compose --project-directory "$BACKEND_DIR" -f "$BACKEND_DIR/docker-compose.yml")
ENDPOINT="${BOOKSPOT_DYNAMODB_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_REGION:-us-east-1}"

usage() {
  cat <<'EOF'
Usage: ./scripts/local-env.sh <start|init|status|smoke|stop|reset>

  start          Start LocalStack, wait for DynamoDB, provision and seed safely
  init           Rerun idempotent table provisioning and seed creation
  status         Show health and DynamoDB tables
  smoke          Verify a write survives a LocalStack restart, then clean it up
  stop           Stop containers but preserve the named data volume
  reset --confirm  Delete the local data volume, then recreate a clean environment
EOF
}

wait_for_localstack() {
  local attempt
  for attempt in $(seq 1 60); do
    if curl -fsS "$ENDPOINT/_localstack/health" >/dev/null 2>&1 &&
       "${COMPOSE[@]}" exec -T -e AWS_DEFAULT_REGION="$REGION" localstack \
         awslocal --region "$REGION" dynamodb list-tables >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  printf 'LocalStack did not become healthy at %s\n' "$ENDPOINT" >&2
  "${COMPOSE[@]}" logs localstack >&2 || true
  return 1
}

init_localstack() {
  wait_for_localstack
  "${COMPOSE[@]}" exec -T \
    -e AWS_DEFAULT_REGION="$REGION" \
    localstack bash /opt/bookspot-init/01-create-dynamodb-tables.sh
}

status_localstack() {
  curl -fsS "$ENDPOINT/_localstack/health"
  printf '\nDynamoDB tables (%s):\n' "$REGION"
  "${COMPOSE[@]}" exec -T \
    -e AWS_DEFAULT_REGION="$REGION" \
    localstack awslocal --region "$REGION" dynamodb list-tables --output table
}

smoke_localstack() {
  init_localstack
  local key="bookspot-smoke-$(date +%s)-$$"
  local item="{\"Id\":{\"S\":\"$key\"},\"Name\":{\"S\":\"Persistence smoke test\"}}"

  cleanup() {
    "${COMPOSE[@]}" exec -T -e AWS_DEFAULT_REGION="$REGION" localstack \
      awslocal --region "$REGION" dynamodb delete-item \
      --table-name services --key "{\"Id\":{\"S\":\"$key\"}}" >/dev/null 2>&1 || true
  }
  trap cleanup EXIT

  "${COMPOSE[@]}" exec -T -e AWS_DEFAULT_REGION="$REGION" localstack \
    awslocal --region "$REGION" dynamodb put-item \
    --table-name services --item "$item" >/dev/null

  "${COMPOSE[@]}" restart localstack >/dev/null
  wait_for_localstack

  local persisted
  persisted=$("${COMPOSE[@]}" exec -T -e AWS_DEFAULT_REGION="$REGION" localstack \
    awslocal --region "$REGION" dynamodb get-item \
    --table-name services --key "{\"Id\":{\"S\":\"$key\"}}" \
    --query 'Item.Id.S' --output text)

  if [[ "$persisted" != "$key" ]]; then
    printf 'Persistence smoke test failed: expected %s, got %s\n' "$key" "$persisted" >&2
    return 1
  fi

  cleanup
  trap - EXIT
  printf 'Persistence smoke test passed: item survived a LocalStack restart and was removed.\n'
}

command="${1:-}"
case "$command" in
  start)
    "${COMPOSE[@]}" up -d localstack
    wait_for_localstack
    init_localstack
    status_localstack
    ;;
  init)
    init_localstack
    ;;
  status)
    wait_for_localstack
    status_localstack
    ;;
  smoke)
    smoke_localstack
    ;;
  stop)
    "${COMPOSE[@]}" down
    ;;
  reset)
    if [[ "${2:-}" != "--confirm" ]]; then
      printf 'Reset deletes all BookSpot LocalStack data. Rerun with: %s reset --confirm\n' "$0" >&2
      exit 2
    fi
    "${COMPOSE[@]}" down --volumes
    "${COMPOSE[@]}" up -d localstack
    wait_for_localstack
    init_localstack
    status_localstack
    ;;
  *)
    usage
    exit 2
    ;;
esac
