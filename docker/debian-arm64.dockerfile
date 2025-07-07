FROM mcr.microsoft.com/dotnet/sdk:9.0.301-bookworm-slim@sha256:b768b444028d3c531de90a356836047e48658cd1e26ba07a539a6f1a052a35d9

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.411 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
