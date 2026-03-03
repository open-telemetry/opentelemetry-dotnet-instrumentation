FROM mcr.microsoft.com/dotnet/sdk:9.0.311-bookworm-slim@sha256:7ba12a329980dad8291cc7339ad073a8666ed949a78138eb8543a8cc8ccaa0de
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.103 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.418 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
