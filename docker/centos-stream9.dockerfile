FROM quay.io/centos/centos:stream9@sha256:680463364408e176d4722de9e8a3ae95b31b5041d47e31399156118cc0627d8f

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "48e5763854527aca84bf2c9b1542a22ec490e85657725eac8dc18eb0ed809413  dotnet-install.sh" | sha256sum -c \
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