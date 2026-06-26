FROM mcr.microsoft.com/dotnet/sdk:9.0.315-bookworm-slim@sha256:24fe7c00719afc72e2b67cace264dea2ee39ae45f5b553bd59493fe3ddfc920f
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

# renovate: datasource=deb depName=cmake
ARG CMAKE_VERSION=3.25.1-1
# renovate: datasource=deb depName=clang
ARG CLANG_VERSION=1:14.0-55.7~deb12u1
# renovate: datasource=deb depName=make
ARG MAKE_VERSION=4.3-4.1

RUN apt-get update && \
    apt-get install -y \
        cmake="${CMAKE_VERSION}" \
        clang="${CLANG_VERSION}" \
        make="${MAKE_VERSION}"

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.301 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.422 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
