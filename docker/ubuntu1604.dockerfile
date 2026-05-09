FROM ubuntu:16.04@sha256:1f1a2d56de1d604801a9671f301190704c25d604a416f59e03c04f5c6ffee0d6

RUN apt-get update && \
    apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    git \
    build-essential software-properties-common \
    gnupg \
    libicu-dev

# Install newer clang
RUN curl -fsSL https://apt.llvm.org/llvm-snapshot.gpg.key | gpg --dearmor -o /usr/share/keyrings/llvm-archive-keyring.gpg && \
    echo 'deb [signed-by=/usr/share/keyrings/llvm-archive-keyring.gpg] https://apt.llvm.org/xenial/ llvm-toolchain-xenial-5.0 main' | tee /etc/apt/sources.list.d/llvm.list >/dev/null && \
    apt-get update && \
    apt-get install -y clang-5.0 && \
    update-alternatives --install /usr/bin/clang++ clang++ /usr/bin/clang++-5.0 1000 && \
    update-alternatives --install /usr/bin/clang clang /usr/bin/clang-5.0 1000 && \
    update-alternatives --config clang && \
    update-alternatives --config clang++

# Install newer g++
RUN add-apt-repository ppa:ubuntu-toolchain-r/test -y && \
    apt-get update && \
    apt-get install -y g++-9 && \
    update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-9 60 --slave /usr/bin/g++ g++ /usr/bin/g++-9

# Install cmake 3.20.5 directly from GitHub releases (Kitware Xenial apt repo no longer serves cmake)
RUN curl -fsSL -o cmake.sh https://github.com/Kitware/CMake/releases/download/v3.20.5/cmake-3.20.5-linux-x86_64.sh && \
    echo "f582e02696ceee81818dc3378531804b2213ed41c2a8bc566253d16d894cefab  cmake.sh" | sha256sum -c - && \
    sh cmake.sh --skip-license --prefix=/usr/local && \
    rm cmake.sh

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.313 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV IsLegacyUbuntu=true

WORKDIR /project
