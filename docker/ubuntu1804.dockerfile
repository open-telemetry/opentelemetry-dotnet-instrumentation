FROM ubuntu:18.04@sha256:152dc042452c496007f07ca9127571cb9c29697f42acbfad72324b2bb2e43c98

RUN apt-get update && \
    apt-get install -y \
    apt-transport-https \
    build-essential \
    ca-certificates \
    clang \
    curl \
    git \
    gnupg \
    libicu-dev \
    software-properties-common

# Install newer g++
RUN add-apt-repository ppa:ubuntu-toolchain-r/test -y && \
    apt-get update && \
    apt-get install -y g++-9 && \
    update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-9 60 --slave /usr/bin/g++ g++ /usr/bin/g++-9

# Install newer CMake. Ubuntu 18.04 provides CMake 3.10, but the native build uses newer commands.
RUN curl -fsSL -o cmake.sh https://github.com/Kitware/CMake/releases/download/v3.20.5/cmake-3.20.5-linux-x86_64.sh && \
    echo "f582e02696ceee81818dc3378531804b2213ed41c2a8bc566253d16d894cefab  cmake.sh" | sha256sum -c - && \
    sh cmake.sh --skip-license --prefix=/usr/local && \
    rm cmake.sh

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 11.0.100-preview.5.26302.115 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
