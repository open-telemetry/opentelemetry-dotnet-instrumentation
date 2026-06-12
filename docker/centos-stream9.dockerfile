FROM quay.io/centos/centos:stream9@sha256:00455a145dbc8b0787f1ae3dca8f64239bbad8a78b4f04f161b6bc9d0508fa63

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.301 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 9.0.315 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.422 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

# https://github.com/dotnet/runtime/issues/65874
RUN update-crypto-policies --set LEGACY

# Install dependencies
RUN dnf install -y \
    cmake-3.31.8-3.el9 \
    clang-21.1.8-2.el9 \
    git-2.52.0-1.el9

WORKDIR /project
