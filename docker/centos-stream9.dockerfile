FROM quay.io/centos/centos:stream9@sha256:8e04bea92071ab80e52828f9b5b8dcbd3604b16c8152b7204a1c9dc9752de34a

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "48e5763854527aca84bf2c9b1542a22ec490e85657725eac8dc18eb0ed809413  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.102 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.405 --install-dir /usr/share/dotnet --no-path \
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