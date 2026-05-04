FROM quay.io/centos/centos:stream9@sha256:d5b319ece56684157ce9a09a857ad7002794159f572c890005cddeb2c5043b1f

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.203 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 9.0.313 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.420 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# Install dependencies
RUN dnf install -y \
    cmake-3.31.8-3.el9 \
    clang-21.1.8-2.el9 \
    git-2.52.0-1.el9

WORKDIR /project
