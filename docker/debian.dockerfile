FROM mcr.microsoft.com/dotnet/sdk:9.0.301-bookworm-slim@sha256:b768b444028d3c531de90a356836047e48658cd1e26ba07a539a6f1a052a35d9

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
