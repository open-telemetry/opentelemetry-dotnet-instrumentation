FROM mcr.microsoft.com/dotnet/sdk:9.0.312-bookworm-slim@sha256:dbbdd47fae7a98f78932eea912a5233d746073ffb485849a5540d9ed135c922d
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.201 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.419 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
