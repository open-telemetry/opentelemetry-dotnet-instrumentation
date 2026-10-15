FROM ubuntu:18.04@sha256:152dc042452c496007f07ca9127571cb9c29697f42acbfad72324b2bb2e43c98

# renovate: datasource=deb depName=clang-12
ARG CLANG_12_VERSION=1:12.0.1~++20211102090516+fed41342a82f-1~exp1~20211102211019.11

#TODO NET11TODO put here exact versions as arguments as in other docker images

RUN apt-get update && \
    apt-get install -y \
    apt-transport-https \
    build-essential \
    ca-certificates \
    curl \
    git \
    gnupg \
    libicu-dev \
    software-properties-common

# Install Clang 12 for C++20 support while retaining GCC 9/libstdc++ 9 compatibility.
RUN curl -fsSL -o /tmp/llvm-snapshot.gpg.key https://apt.llvm.org/llvm-snapshot.gpg.key && \
    echo "8b2a587ffd672c4687e7581dad4b2f6c1bb2ad6b480cd9771ba2ff48e0b8c75d  /tmp/llvm-snapshot.gpg.key" | sha256sum -c - && \
    gpg --dearmor -o /usr/share/keyrings/llvm-archive-keyring.gpg /tmp/llvm-snapshot.gpg.key && \
    rm /tmp/llvm-snapshot.gpg.key && \
    echo 'deb [signed-by=/usr/share/keyrings/llvm-archive-keyring.gpg] https://apt.llvm.org/bionic/ llvm-toolchain-bionic-12 main' > /etc/apt/sources.list.d/llvm.list && \
    apt-get update && \
    apt-get install -y clang-12="${CLANG_12_VERSION}" && \
    update-alternatives --install /usr/bin/clang clang /usr/bin/clang-12 1000 --slave /usr/bin/clang++ clang++ /usr/bin/clang++-12

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
    && ./dotnet-install.sh -v 11.0.100-rc.2.26504.105 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
