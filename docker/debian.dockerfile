FROM mcr.microsoft.com/dotnet/sdk:9.0.316-bookworm-slim@sha256:ee0d20fbc3dfc60ae2d9d9115a9b750c18576a47f24073119dd09d93e072cb89
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via apt-get

# renovate: datasource=deb depName=cmake
ARG CMAKE_VERSION=3.25.1-1
# renovate: datasource=deb depName=clang
ARG CLANG_VERSION=1:14.0-55.7~deb12u1
# renovate: datasource=deb depName=make
ARG MAKE_VERSION=4.3-4.1

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-10.0 \
        dotnet-sdk-8.0 \
        cmake="${CMAKE_VERSION}" \
        clang="${CLANG_VERSION}" \
        make="${MAKE_VERSION}"

WORKDIR /project
