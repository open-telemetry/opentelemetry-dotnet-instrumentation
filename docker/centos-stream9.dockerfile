FROM quay.io/centos/centos:stream9@sha256:4fdfb367402535024cd7703a67e4549ca3b0a0ee05081e06d89e24dce3a3827a

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.402 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 9.0.319 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.426 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# renovate: datasource=rpm depName=cmake
ARG CMAKE_VERSION=3.31.8-3.el9
# renovate: datasource=rpm depName=clang
ARG CLANG_VERSION=22.1.8-1.el9
# renovate: datasource=rpm depName=git
ARG GIT_VERSION=2.52.0-1.el9

# Install dependencies
RUN dnf install -y \
    cmake-"${CMAKE_VERSION}" \
    clang-"${CLANG_VERSION}" \
    git-"${GIT_VERSION}"

WORKDIR /project
