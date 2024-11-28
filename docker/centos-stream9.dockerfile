FROM quay.io/centos/centos:stream9@sha256:fc94f4a0545cac9d6ea76e087b1482ea12b7166a35ffde69eb9708d2e17af148

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