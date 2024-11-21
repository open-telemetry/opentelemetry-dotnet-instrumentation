#!/bin/bash
set -ex

# install .NET tools
dotnet tool restore

# install .NET 9.0 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh
chmod +x ./dotnet-install.sh
sudo ./dotnet-install.sh -c 9.0 --install-dir /usr/share/dotnet --no-path
rm dotnet-install.sh
