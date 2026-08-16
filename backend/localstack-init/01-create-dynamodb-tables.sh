#!/usr/bin/env bash
set -euo pipefail

REGION="${AWS_DEFAULT_REGION:-us-east-1}"
AWS=(awslocal --region "$REGION" dynamodb)

log() {
  printf '[bookspot-init] %s\n' "$*"
}

create_table_if_missing() {
  local table_name="$1"
  local key_name="$2"
  local description
  local actual_hash_key
  local actual_key_type
  local key_count
  local create_output

  if description=$("${AWS[@]}" describe-table --table-name "$table_name" 2>/dev/null); then
    actual_hash_key=$(printf '%s' "$description" | python3 -c \
      'import json,sys; keys=json.load(sys.stdin)["Table"]["KeySchema"]; print(next((item["AttributeName"] for item in keys if item["KeyType"] == "HASH"), ""))')
    key_count=$(printf '%s' "$description" | python3 -c \
      'import json,sys; print(len(json.load(sys.stdin)["Table"]["KeySchema"]))')
    actual_key_type=$(printf '%s' "$description" | python3 -c \
      'import json,sys; table=json.load(sys.stdin)["Table"]; key=sys.argv[1]; print(next((item["AttributeType"] for item in table["AttributeDefinitions"] if item["AttributeName"] == key), ""))' "$key_name")
    if [[ "$actual_hash_key" != "$key_name" || "$actual_key_type" != "S" || "$key_count" != "1" ]]; then
      printf "[bookspot-init] table '%s' has incompatible KeySchema; expected one string HASH key named '%s'. Use the explicit local reset after preserving any needed data.\n" \
        "$table_name" "$key_name" >&2
      return 1
    fi
    log "table '$table_name' already exists with the expected key schema"
    return
  fi

  log "creating table '$table_name' (partition key: $key_name)"
  if ! create_output=$("${AWS[@]}" create-table \
      --table-name "$table_name" \
      --attribute-definitions "AttributeName=$key_name,AttributeType=S" \
      --key-schema "AttributeName=$key_name,KeyType=HASH" \
      --billing-mode PAY_PER_REQUEST 2>&1); then
    if [[ "$create_output" == *ResourceInUseException* ]]; then
      log "table '$table_name' was created concurrently; waiting and validating it"
      "${AWS[@]}" wait table-exists --table-name "$table_name"
      create_table_if_missing "$table_name" "$key_name"
      return
    fi
    printf '%s\n' "$create_output" >&2
    return 1
  fi
  "${AWS[@]}" wait table-exists --table-name "$table_name"
}

put_seed_if_missing() {
  local table_name="$1"
  local key_name="$2"
  local item="$3"
  local output

  if output=$("${AWS[@]}" put-item \
      --table-name "$table_name" \
      --item "$item" \
      --condition-expression 'attribute_not_exists(#pk)' \
      --expression-attribute-names "{\"#pk\":\"$key_name\"}" 2>&1); then
    log "created seed item in '$table_name'"
  elif [[ "$output" == *ConditionalCheckFailedException* ]]; then
    log "seed item in '$table_name' already exists; preserving it"
  else
    printf '%s\n' "$output" >&2
    return 1
  fi
}

log "provisioning DynamoDB in region $REGION"
create_table_if_missing profiles Id
create_table_if_missing businesses Id
create_table_if_missing services Id
create_table_if_missing business_hours Id
create_table_if_missing bookings Id
create_table_if_missing reviews Id
create_table_if_missing password_reset_tokens Token
create_table_if_missing identity_claims ClaimKey
create_table_if_missing booking_reservations ReservationKey
create_table_if_missing booking_audit AuditKey

# Stable IDs make the sample relationship easy to inspect. Conditional writes ensure
# a developer's edits to these records are never overwritten by a rerun.
put_seed_if_missing profiles Id '{"Id":{"S":"local-provider-001"},"Email":{"S":"provider.local@bookspot.test"},"FullName":{"S":"Local BookSpot Provider"},"UserType":{"S":"provider"},"PasswordHash":{"S":""},"CreatedAt":{"S":"2026-01-01T00:00:00.000Z"}}'
put_seed_if_missing businesses Id '{"Id":{"S":"local-business-001"},"ProviderId":{"S":"local-provider-001"},"BusinessName":{"S":"BookSpot Local Studio"},"Description":{"S":"Deterministic LocalStack seed data"},"Address":{"S":"1 Developer Way"},"Phone":{"S":"+10000000000"},"Email":{"S":"studio.local@bookspot.test"},"City":{"S":"Localhost"},"IsActive":{"BOOL":true},"CreatedAt":{"S":"2026-01-01T00:00:00.000Z"}}'
put_seed_if_missing services Id '{"Id":{"S":"local-service-001"},"BusinessId":{"S":"local-business-001"},"ProviderId":{"S":"local-provider-001"},"ProviderName":{"S":"Local BookSpot Provider"},"Name":{"S":"LocalStack Test Service"},"Description":{"S":"Seed service for local API smoke tests"},"Category":{"S":"Development"},"Price":{"N":"25.00"},"DurationMinutes":{"N":"30"},"Tags":{"L":[{"S":"local"},{"S":"seed"}]},"Location":{"S":"Localhost"},"IsActive":{"BOOL":true},"CreatedAt":{"S":"2026-01-01T00:00:00.000Z"}}'

log "ready: $("${AWS[@]}" list-tables --query 'TableNames' --output text)"
