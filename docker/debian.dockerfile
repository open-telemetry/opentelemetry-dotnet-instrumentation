FROM mcr.microsoft.com/dotnet/sdk:9.0.102-bookworm-slim@sha256:84fd557bebc64015e731aca1085b92c7619e49bdbe247e57392a43d92276f617

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-8.0 \
        cmake \
        clang \
        make

WORKDIR /project
