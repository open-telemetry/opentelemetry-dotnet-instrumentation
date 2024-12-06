FROM quay.io/centos/centos:stream9@sha256:83c3a9ae23561bdea27e0a10b4321b6186564850cbfbaac722a15e4c8a58c09e

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "c169af55281cd1e58cdbe3ec95c2480cfb210ee460b3ff1421745c8f3236b263  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.101 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.404 --install-dir /usr/share/dotnet --no-path \
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