#!/bin/bash
set -euxo pipefail

uname_os() {
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    case "$os" in
        cygwin_nt*) echo "windows" ;;
        mingw*) echo "windows" ;;
        msys_nt*) echo "windows" ;;
        *) echo "$os" ;;
    esac
}

port=$1

serverPort() {
  netstat -ano | grep $port
}

waitForServer() {
    os=$(uname_os)
    case "$os" in
        windows*)
          while [ "" ==  "$(serverPort)" ]; do   
            sleep 0.5
          done
          ;;
        *)
          while ! nc -z localhost $port; do   
           sleep 0.5
          done
          ;;
    esac
} &> /dev/null

echo "Waiting on port $port ..."
time $(waitForServer) 
echo "Port detected"
