FROM mcr.microsoft.com/dotnet/sdk:9.0.301-bookworm-slim@sha256:faa2daf2b72cbe787ee1882d9651fa4ef3e938ee56792b8324516f5a448f3abe

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        gpg \
        make

# Import the GPG key to verify the .NET installation script
# See https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script#signature-validation-of-dotnet-installsh
RUN curl -sSL --retry 5 https://dot.net/v1/dotnet-install.asc --output dotnet-install.asc && \
    gpg --import dotnet-install.asc && \
    rm dotnet-install.asc

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sig --output dotnet-install.sig \
    && gpg --verify dotnet-install.sig dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.411 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh \
    && rm dotnet-install.sig

WORKDIR /project
