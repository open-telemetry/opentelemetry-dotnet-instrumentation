FROM quay.io/centos/centos:stream9@sha256:e0946abf1c5cba2efd264ef8952b26e604651d9e2c584a292d374f5c954f90c2

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "19b0a7890c371201b944bf0f8cdbb6460d053d63ddbea18cfed3e4199769ce17  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.202 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.407 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# Install dependencies
RUN dnf install -y \
    cmake-3.26.5-2.el9 \
    clang-18.1.8-3.el9 \
    git-2.43.5-1.el9

WORKDIR /project