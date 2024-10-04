FROM quay.io/centos/centos:stream9

# Install dotnet sdk
RUN dnf install dotnet-sdk-8.0 \
    dnf install dotnet-sdk-7.0 \
    dnf install dotnet-sdk-6.0

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-pololicies --set LEGACY

# Install dependencies
RUN dnf install cmake-3.26.5-2.el9 \
    clang-18.1.8-3.el9

WORKDIR /project