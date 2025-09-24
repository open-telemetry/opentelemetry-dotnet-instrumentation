FROM quay.io/centos/centos:stream9@sha256:825b87d925b773f17b2a513db972f2c4a5cb2af9002d537dd20eb0ce0a28a9fd

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.305 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.414 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# Install dependencies
RUN dnf install -y \
    cmake-3.26.5-2.el9 \
    clang-20.1.3-1.el9 \
    git-2.43.5-1.el9

WORKDIR /project
