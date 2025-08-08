FROM mcr.microsoft.com/dotnet/sdk:9.0.304-bookworm-slim@sha256:840f3b62b9742dde4461a3c31e38ffd34d41d7d33afd39c378cfcfd5dcb82bd5

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.412 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
