FROM mcr.microsoft.com/dotnet/sdk:9.0.308-bookworm-slim@sha256:4dceb990b7f76ae0b2f4e4fb1ac3485c3a354fb9fdaed767edb7590273508602
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.100 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.416 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
