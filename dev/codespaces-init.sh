#!/bin/bash
set -e

# install Nuke globally
dotnet tool install Nuke.GlobalTool --global

# install .NET 6.0 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh
chmod +x ./dotnet-install.sh
sudo ./dotnet-install.sh -c 6.0 --install-dir /usr/share/dotnet --no-path
rm dotnet-install.sh
