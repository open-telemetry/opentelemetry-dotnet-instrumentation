FROM quay.io/centos/centos:stream9

# Install dotnet sdk
RUN dnf install -y \
    dotnet-sdk-8.0 \
    dotnet-sdk-7.0 \
    dotnet-sdk-6.0

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# Install dependencies
RUN dnf install -y \
    cmake-3.26.5-2.el9 \
    clang-18.1.8-3.el9 \
    git-2.43.5-1.el9

WORKDIR /project