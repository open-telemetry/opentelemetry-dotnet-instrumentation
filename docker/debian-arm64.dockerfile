FROM mcr.microsoft.com/dotnet/sdk:9.0.306-bookworm-slim@sha256:a5dd7352c0c058a6f847c95a1147b060e95a532444f14b34d8fa9aaa0a76702f

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.415 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
