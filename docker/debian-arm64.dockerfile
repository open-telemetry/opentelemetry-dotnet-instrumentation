FROM mcr.microsoft.com/dotnet/sdk:9.0.314-bookworm-slim@sha256:087fc98e5c6ffcea6c3e276c135c4a6717c589d9509a09cc22e7c634830a4db8
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.300 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.421 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
