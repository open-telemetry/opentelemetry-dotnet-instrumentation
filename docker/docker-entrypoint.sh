#!/usr/bin/env bash
set -e

#enable software collection
source /opt/rh/devtoolset-9/enable
exec "$@"
