#!/bin/bash
set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd $DIR/.. >/dev/null
source $DIR/envvars.sh
cd - >/dev/null

eval "$@"
