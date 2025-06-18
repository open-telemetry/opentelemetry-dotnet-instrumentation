FROM quay.io/centos/centos:stream9@sha256:4c755d11df35a63dc8dc0155cfa00713040b76bb8737ac60e608c7111e1de589

# Install dotnet sdk
RUN dnf install -y \
    libicu-devel

RUN curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA512: $(sha512sum dotnet-install.sh)" \
    && echo "f8c59166ed912d6861e93c3efc2840be31ec32897679678a72f781423ebf061348d3b92b16c9541f5b312a34160f452826bb3021efb1414d76bd7e237e4c0e9a  dotnet-install.sh" | sha512sum -c \
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
