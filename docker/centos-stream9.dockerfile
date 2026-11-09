FROM quay.io/centos/centos:stream9@sha256:99a7748660352b7d87e5689805ddafa6209d6019d0c5b642b5354656dff02be3

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.403 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 9.0.320 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.427 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# renovate: datasource=rpm depName=cmake
ARG CMAKE_VERSION=3.31.8-3.el9
# renovate: datasource=rpm depName=clang
ARG CLANG_VERSION=23.1.3-1.el9
# renovate: datasource=rpm depName=git
ARG GIT_VERSION=2.52.0-1.el9

# Install dependencies
RUN dnf install -y \
    cmake-"${CMAKE_VERSION}" \
    clang-"${CLANG_VERSION}" \
    git-"${GIT_VERSION}"

WORKDIR /project
