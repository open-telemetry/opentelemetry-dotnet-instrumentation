FROM quay.io/centos/centos:stream9@sha256:dfc4616f518ab5f0a6e95a6cb168dadbed4704b7b30d3a10846bc2c4c1c61f07

# Install dotnet sdk
RUN dnf install -y \
    dotnet-sdk-9.0 \
    dotnet-sdk-8.0

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# Install dependencies
RUN dnf install -y \
    cmake-3.26.5-2.el9 \
    clang-18.1.8-3.el9 \
    git-2.43.5-1.el9

WORKDIR /project