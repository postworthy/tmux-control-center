#!/usr/bin/env bash
# Sourced by recovery and its tests; keep BSD/GNU differences out of state logic.
workspace_stat_owner() {
  if [[ $(uname -s) == Darwin ]]; then stat -f '%u' "$1"; else stat -c '%u' -- "$1"; fi
}
workspace_stat_mode() {
  if [[ $(uname -s) == Darwin ]]; then stat -f '%Lp' "$1"; else stat -c '%a' -- "$1"; fi
}
workspace_base64_decode() {
  if [[ $(uname -s) == Darwin ]]; then base64 -D; else base64 -d; fi
}
