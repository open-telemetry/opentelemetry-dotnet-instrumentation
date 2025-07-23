FROM mcr.microsoft.com/dotnet/sdk:9.0.303-bookworm-slim@sha256:86fe223b90220ec8607652914b1d7dc56fc8ff422ca1240bb81e54c4b06509e6

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
