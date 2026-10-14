#!/usr/bin/env bash
# Copies the search API keys (Search:ApiKey:*) from another project's user-secrets
# store into this project's user secrets. Values travel only through a pipe:
# never argv, stdout, logs or files in the repo. Only key names are printed.
#
# Usage: scripts/copy-search-secrets.sh [source-user-secrets-id]
#   default source: vinted-offers-7c1f4b52 (vinted-offers/src/VintedOffers.Web)
set -euo pipefail
set +x

source_id="${1:-vinted-offers-7c1f4b52}"
prefix="Search:ApiKey:"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
target_project="$repo_root/src/AiHeroRat"
source_file="$HOME/.microsoft/usersecrets/$source_id/secrets.json"

command -v jq >/dev/null || { echo "error: jq is required" >&2; exit 1; }
[[ -f "$source_file" ]] || { echo "error: no user-secrets store for id '$source_id'" >&2; exit 1; }

keys="$(jq -r --arg p "$prefix" 'keys[] | select(startswith($p))' "$source_file")"
[[ -n "$keys" ]] || { echo "error: no '$prefix*' keys in '$source_id'" >&2; exit 1; }

jq --arg p "$prefix" 'with_entries(select(.key | startswith($p)))' "$source_file" \
  | dotnet user-secrets set --project "$target_project" >/dev/null

echo "Copied from '$source_id' into $(basename "$target_project") user secrets:"
printf '  %s\n' $keys
