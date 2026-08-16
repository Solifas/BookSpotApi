#!/usr/bin/env python3
"""Static regression checks for the reproducible BookSpot local environment."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

BACKEND = Path(__file__).resolve().parents[1]
EXPECTED_TABLE_KEYS = {
    "profiles": "Id",
    "businesses": "Id",
    "services": "Id",
    "business_hours": "Id",
    "bookings": "Id",
    "reviews": "Id",
    "password_reset_tokens": "Token",
    "identity_claims": "ClaimKey",
    "booking_reservations": "ReservationKey",
    "booking_audit": "AuditKey",
}
CANONICAL_REGION = "us-east-1"


def require(condition: bool, message: str, failures: list[str]) -> None:
    if not condition:
        failures.append(message)


def main() -> int:
    failures: list[str] = []
    compose = (BACKEND / "docker-compose.yml").read_text(encoding="utf-8")
    init = (BACKEND / "localstack-init" / "01-create-dynamodb-tables.sh").read_text(encoding="utf-8")
    program = (BACKEND / "BookSpot.API" / "Program.cs").read_text(encoding="utf-8")
    readme = (BACKEND / "README.md").read_text(encoding="utf-8")
    settings = json.loads((BACKEND / "BookSpot.API" / "appsettings.Development.json").read_text(encoding="utf-8"))

    require("localstack/localstack:latest" not in compose, "LocalStack image must be pinned", failures)
    require(bool(re.search(r"localstack/localstack:\d+\.\d+\.\d+", compose)), "Compose must use a semantic LocalStack tag", failures)
    require("127.0.0.1:4566:4566" in compose, "LocalStack must bind only to host loopback", failures)
    require("/var/lib/localstack" in compose, "Compose must persist /var/lib/localstack", failures)
    require("healthcheck:" in compose, "Compose must define a LocalStack health check", failures)
    require("awslocal" in compose and "list-tables" in compose, "Compose health must probe DynamoDB, not only the gateway", failures)
    require("/etc/localstack/init/ready.d" not in compose, "Provisioning must have one owner; do not race an init hook with the host script", failures)
    require("/opt/bookspot-init" in compose, "Compose must mount the explicit host-owned provisioner", failures)
    require(bool(re.search(r"AWS_DEFAULT_REGION:\s*[\"']?us-east-1", compose)), "Compose region must be canonical", failures)

    for table, key in EXPECTED_TABLE_KEYS.items():
        pair = rf"create_table_if_missing\s+{re.escape(table)}\s+{re.escape(key)}(?:\s|$)"
        require(bool(re.search(pair, init)), f"Provisioner is missing exact schema pair {table}/{key}", failures)
    require("describe-table" in init, "Provisioning must detect existing tables", failures)
    require("KeySchema" in init, "Provisioning must validate existing table key schemas", failures)
    require("AttributeDefinitions" in init and "actual_key_type" in init, "Provisioning must validate the partition-key attribute type", failures)
    require("ResourceInUseException" in init, "Provisioning must tolerate concurrent already-exists races", failures)
    require("attribute_not_exists" in init, "Seed writes must preserve existing records", failures)

    require(settings.get("AWS", {}).get("Region") == CANONICAL_REGION, "Development AWS region must be canonical", failures)
    require(settings.get("AWS", {}).get("ServiceURL") == "http://localhost:4566", "Development endpoint must be LocalStack", failures)
    for token in ("AWS:ServiceURL", "AWS:Region", "AWS:AccessKey", "AWS:SecretKey", "AuthenticationRegion"):
        require(token in program, f"Program.cs must use configurable {token}", failures)

    for command in ("bash ./scripts/local-env.sh start", "bash ./scripts/local-env.sh smoke", "bash ./scripts/local-env.sh reset", "bash ./scripts/run-api.sh", "dotnet run"):
        require(command in readme, f"README must document {command}", failures)
    require("> /dev/null" not in readme, "README health checks must not use a non-portable Windows curl redirect", failures)

    if failures:
        print("Local setup verification FAILED:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    print("Local setup verification passed.")
    print(f"Validated {len(EXPECTED_TABLE_KEYS)} tables, region {CANONICAL_REGION}, LocalStack endpoint http://localhost:4566.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
