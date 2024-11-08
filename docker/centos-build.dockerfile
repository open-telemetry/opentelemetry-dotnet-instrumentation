FROM centos:centos7.9.2009@sha256:be65f488b7764ad3638f236b7b515b3678369a5124c47b8d32916d6487418ea4

RUN yum update -y \
    && yum -y install centos-release-scl-2-3.el7.centos \
    && yum -y install make-3.82-24.el7 \
    && yum -y install git-1.8.3.1-24.el7_9 \
    # contains recent versions of gcc
    && yum -y install devtoolset-9-9.1-0.el7 \
    # required to build llvm
    && yum -y install python3-3.6.8-18.el7

RUN curl -sL https://cmake.org/files/v3.25/cmake-3.25.2-linux-x86_64.sh -o cmake_install.sh \
    && chmod +x cmake_install.sh \
    && ./cmake_install.sh --prefix=/usr/local --exclude-subdir --skip-license \
    && rm cmake_install.sh

# https://releases.llvm.org/12.0.0/docs/GettingStarted.html#getting-started-quickly-a-summary
RUN git clone --depth 1 --branch release/12.x https://github.com/llvm/llvm-project.git \
    && cd llvm-project \
    && mkdir build \
    && cd build \
    # enable software collections with recent versions of gcc required to build llvm,
    # use gcc_install_prefix to tell clang where gcc containing required libstdc++ is installed
    && scl enable devtoolset-9 -- cmake -DLLVM_ENABLE_PROJECTS=clang -DCMAKE_BUILD_TYPE=Release -DGCC_INSTALL_PREFIX=/opt/rh/devtoolset-9/root/usr/ -DLLVM_TEMPORARILY_ALLOW_OLD_TOOLCHAIN=1 -G "Unix Makefiles" ../llvm \
    && make install \
    && cd ../../ \
    && rm -rf llvm-project
