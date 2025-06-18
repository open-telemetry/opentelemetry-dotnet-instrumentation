FROM quay.io/centos/centos:stream9@sha256:45650b7974762418b66987d67c063aee0d2fab0ac8fade2db9807b3ec4bbd1af

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

RUN curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "19b0a7890c371201b944bf0f8cdbb6460d053d63ddbea18cfed3e4199769ce17  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.301 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.411 --install-dir /usr/share/dotnet --no-path \
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
