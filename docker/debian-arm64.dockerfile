FROM mcr.microsoft.com/dotnet/sdk:9.0.303-bookworm-slim@sha256:670ef9e8eca44c8baa0bd1c229ccde9537064260ef14d54738b7a87916609312

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
