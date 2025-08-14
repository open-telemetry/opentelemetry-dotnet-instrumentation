FROM mcr.microsoft.com/dotnet/sdk:9.0.304-bookworm-slim@sha256:ae000be75dac94fc40e00f0eee903289e985995cc06dac3937469254ce5b60b6

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
