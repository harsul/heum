#!/usr/bin/env bash
# Usage: ./rename.sh <NewName>
# Renames the "Heum" project token to your chosen name.
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <NewName>" >&2
  exit 1
fi

NEW_PASCAL="$1"
NEW_LOWER="${NEW_PASCAL,,}"
OLD_PASCAL="Heum"
OLD_LOWER="heum"
ROOT="$(cd "$(dirname "$0")" && pwd)"

EXTS=".cs .json .csproj .slnx .sln .ts .tsx .ps1 .sh .yml .yaml .md .props .targets .txt .editorconfig"

echo "Rewriting file contents..."
while IFS= read -r -d '' file; do
  if grep -qF "$OLD_PASCAL" "$file" 2>/dev/null || grep -qF "$OLD_LOWER" "$file" 2>/dev/null; then
    sed -i "s/$OLD_PASCAL/$NEW_PASCAL/g; s/$OLD_LOWER/$NEW_LOWER/g" "$file"
    echo "  updated $file"
  fi
done < <(find "$ROOT" \
  -not \( -path "$ROOT/obj/*" -prune \) \
  -not \( -path "$ROOT/bin/*" -prune \) \
  -not \( -path "$ROOT/node_modules/*" -prune \) \
  -not \( -path "$ROOT/.git/*" -prune \) \
  \( -name "Dockerfile" $(for ext in $EXTS; do printf -- "-o -name '*%s' " "$ext"; done) \) \
  -type f -print0 2>/dev/null)

echo "Renaming files and directories..."
find "$ROOT" \
  -not \( -path "$ROOT/.git" -prune \) \
  -not \( -path "*/node_modules/*" -prune \) \
  -not \( -path "*/obj/*" -prune \) \
  -not \( -path "*/bin/*" -prune \) \
  | sort -r \
  | while IFS= read -r item; do
    base="$(basename "$item")"
    newbase="${base//$OLD_PASCAL/$NEW_PASCAL}"
    newbase="${newbase//$OLD_LOWER/$NEW_LOWER}"
    if [[ "$newbase" != "$base" ]]; then
      dir="$(dirname "$item")"
      mv "$item" "$dir/$newbase"
      echo "  renamed $base -> $newbase"
    fi
  done

echo "Done. Review the changes with 'git diff --stat'."
