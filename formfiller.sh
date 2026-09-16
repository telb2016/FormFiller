#!/usr/bin/env bash
# Linux-native FormFiller launcher (happy path — no Wine).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INI_DIR="${ROOT}/profiles"
SMOKE=0
INI=""

usage() {
  cat <<USAGE
Usage: ./formfiller.sh [--smoke] [profile.ini]

  With no args: pick/create a profile under ./profiles, then run Playwright.
  --smoke       INI round-trip only (no browser).
  profile.ini   Path or name under ./profiles (created if missing).

Examples:
  ./formfiller.sh --smoke
  ./formfiller.sh work.ini
  ./formfiller.sh --smoke profiles/work.ini
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help) usage; exit 0 ;;
    --smoke) SMOKE=1; shift ;;
    -*) echo "Unknown flag: $1" >&2; usage; exit 2 ;;
    *) INI="$1"; shift ;;
  esac
done

mkdir -p "$INI_DIR"

if [[ -z "$INI" ]]; then
  if [[ -t 0 ]]; then
    echo "Profiles in ${INI_DIR}:"
    mapfile -t files < <(find "$INI_DIR" -maxdepth 1 -type f -name '*.ini' | sort)
    if ((${#files[@]})); then
      i=1
      for f in "${files[@]}"; do
        printf "  %d) %s\n" "$i" "$(basename "$f")"
        ((i++)) || true
      done
      printf "  n) new profile\n"
      read -r -p "Choose [n]: " choice
      if [[ "$choice" =~ ^[0-9]+$ ]] && (( choice >= 1 && choice <= ${#files[@]} )); then
        INI="${files[$((choice-1))]}"
      else
        read -r -p "New profile name (without .ini): " name
        name="${name:-default}"
        INI="${INI_DIR}/${name%.ini}.ini"
      fi
    else
      read -r -p "New profile name (without .ini) [default]: " name
      name="${name:-default}"
      INI="${INI_DIR}/${name%.ini}.ini"
    fi
  else
    INI="${INI_DIR}/default.ini"
  fi
else
  if [[ "$INI" != /* && "$INI" != ./* && "$INI" != ../* ]]; then
    [[ "$INI" == *.ini ]] || INI="${INI}.ini"
    INI="${INI_DIR}/${INI}"
  fi
fi

cd "$ROOT"
if [[ ! -f FormFiller.csproj ]]; then
  echo "FormFiller.csproj not found in ${ROOT}" >&2
  exit 1
fi

ARGS=(--ini "$INI")
(( SMOKE )) && ARGS+=(--smoke)

echo "Running: dotnet run --configuration Release -- ${ARGS[*]}"
exec dotnet run --configuration Release --no-launch-profile -- "${ARGS[@]}"
