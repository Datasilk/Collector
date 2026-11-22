#!/bin/bash
# Installs Playwright browser binaries + system deps inside a running
# Charlotte container (mounted at /app). Browser binaries persist in the
# playwright-browsers volume, so `install` is a fast no-op once present
# (and updates the browser when the required revision changes).
#
# System deps land in the container's writable layer, which a recreate
# wipes - so we mark them with a sentinel file. Fresh container = no
# sentinel = run the apt install once; existing container = skip it.
set -e

# prefer the bundled linux-x64 driver; csproj publishes every platform's
# node under .playwright so don't grab win32_x64/darwin by accident
NODE=/app/.playwright/node/linux-x64/node
CLI=/app/.playwright/package/cli.js
if [ ! -f "$NODE" ] || [ ! -f "$CLI" ]; then
    NODE=$(find /app/.playwright -type f -name node -path '*linux*' | head -1)
    CLI=$(find /app/.playwright -name cli.js | head -1)
fi

if [ -z "$NODE" ] || [ -z "$CLI" ]; then
    echo "playwright driver not found under /app/.playwright" >&2
    exit 1
fi

# bind-mounted Windows files may lose the exec bit
chmod +x "$NODE" || true

SENTINEL=/var/lib/collector-pw-deps-installed
if [ -f "$SENTINEL" ]; then
    # deps already apt-installed in this container - just ensure the
    # browser revision matches the playwright package (fast no-op when
    # already present, updates when the package version moved)
    exec "$NODE" "$CLI" install chromium
else
    "$NODE" "$CLI" install --with-deps chromium
    touch "$SENTINEL"
fi
