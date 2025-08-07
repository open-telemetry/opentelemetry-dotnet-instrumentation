FROM mcr.microsoft.com/dotnet/sdk:9.0.303-bookworm-slim@sha256:86fe223b90220ec8607652914b1d7dc56fc8ff422ca1240bb81e54c4b06509e6

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.412 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
