FROM mcr.microsoft.com/dotnet/sdk:10.0.301-alpine3.23@sha256:fac7cce841f78faa4bca416fb4c636d1a129c09abd9b50e9b45664b95fd008a0

# renovate: datasource=repology depName=clang21
ARG CLANG21_VERSION=21.1.2-r2
# renovate: datasource=repology depName=cmake
ARG CMAKE_VERSION=4.1.3-r0
# renovate: datasource=repology depName=make
ARG MAKE_VERSION=4.4.1-r3
# renovate: datasource=repology depName=bash
ARG BASH_VERSION=5.3.3-r1
# renovate: datasource=repology depName=alpine-sdk
ARG ALPINE_SDK_VERSION=1.1-r0
# renovate: datasource=repology depName=protobuf
ARG PROTOBUF_VERSION=31.1-r1
# renovate: datasource=repology depName=protobuf-dev
ARG PROTOBUF_DEV_VERSION=31.1-r1
# renovate: datasource=repology depName=grpc
ARG GRPC_VERSION=1.76.0-r2
# renovate: datasource=repology depName=grpc-plugins
ARG GRPC_PLUGINS_VERSION=1.76.0-r2

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang21="${CLANG21_VERSION}" \
        cmake="${CMAKE_VERSION}" \
        make="${MAKE_VERSION}" \
        bash="${BASH_VERSION}" \
        alpine-sdk="${ALPINE_SDK_VERSION}" \
        protobuf="${PROTOBUF_VERSION}" \
        protobuf-dev="${PROTOBUF_DEV_VERSION}" \
        grpc="${GRPC_VERSION}" \
        grpc-plugins="${GRPC_PLUGINS_VERSION}"

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.315 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.422 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
