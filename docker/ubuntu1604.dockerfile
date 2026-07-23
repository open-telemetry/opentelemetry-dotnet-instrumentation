FROM ubuntu:16.04@sha256:1f1a2d56de1d604801a9671f301190704c25d604a416f59e03c04f5c6ffee0d6

# renovate: datasource=deb depName=clang-5.0
ARG CLANG_5_VERSION=1:5.0.2~svn328729-1~exp1~20180509124008.99
# renovate: datasource=deb depName=g++-9
ARG GXX_9_VERSION=9.4.0-1ubuntu1~16.04
ARG UBUNTU_TOOLCHAIN_R_FINGERPRINT=C8EC952E2A0E1FBDC5090F6A2C277A0A352154E5
# renovate: datasource=github-releases depName=Kitware/CMake
ARG CMAKE_VERSION=3.20.5

RUN apt-get update && \
    apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    git \
    build-essential \
    gnupg \
    libicu-dev

# Install newer clang
RUN curl -fsSL https://apt.llvm.org/llvm-snapshot.gpg.key | gpg --dearmor -o /usr/share/keyrings/llvm-archive-keyring.gpg && \
    echo 'deb [signed-by=/usr/share/keyrings/llvm-archive-keyring.gpg] https://apt.llvm.org/xenial/ llvm-toolchain-xenial-5.0 main' | tee /etc/apt/sources.list.d/llvm.list >/dev/null && \
    apt-get update && \
    apt-get install -y clang-5.0="${CLANG_5_VERSION}" && \
    update-alternatives --install /usr/bin/clang++ clang++ /usr/bin/clang++-5.0 1000 && \
    update-alternatives --install /usr/bin/clang clang /usr/bin/clang-5.0 1000 && \
    update-alternatives --config clang && \
    update-alternatives --config clang++

# Install newer g++
RUN curl -fsSL "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x${UBUNTU_TOOLCHAIN_R_FINGERPRINT}" | gpg --dearmor -o /usr/share/keyrings/ubuntu-toolchain-r-archive-keyring.gpg && \
    echo 'deb [signed-by=/usr/share/keyrings/ubuntu-toolchain-r-archive-keyring.gpg] https://ppa.launchpadcontent.net/ubuntu-toolchain-r/test/ubuntu xenial main' > /etc/apt/sources.list.d/ubuntu-toolchain-r.list && \
    apt-get update && \
    apt-get install -y g++-9="${GXX_9_VERSION}" && \
    update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-9 60 --slave /usr/bin/g++ g++ /usr/bin/g++-9

# Install cmake directly from GitHub releases (Kitware Xenial apt repo no longer serves cmake)
RUN CMAKE_INSTALLER="cmake-${CMAKE_VERSION}-linux-x86_64.sh" && \
    CMAKE_INSTALLER_SHA256="f582e02696ceee81818dc3378531804b2213ed41c2a8bc566253d16d894cefab" && \
    curl -fsSL -O "https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/${CMAKE_INSTALLER}" && \
    echo "${CMAKE_INSTALLER_SHA256}  ${CMAKE_INSTALLER}" | sha256sum -c - && \
    sh "${CMAKE_INSTALLER}" --skip-license --prefix=/usr/local && \
    rm "${CMAKE_INSTALLER}"

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.316 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV IsLegacyUbuntu=true

WORKDIR /project
