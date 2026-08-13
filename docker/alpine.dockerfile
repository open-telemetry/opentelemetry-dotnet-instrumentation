FROM mcr.microsoft.com/dotnet/sdk:11.0.100-preview.7-alpine3.24@sha256:186cbf87f5b66f2e4ff937b6a3cd420b005356e940eacd5c420fc30308c49c46

# renovate: datasource=repology depName=clang22
ARG CLANG22_VERSION=22.1.3-r2
# renovate: datasource=repology depName=cmake
ARG CMAKE_VERSION=4.2.3-r0
# renovate: datasource=repology depName=make
ARG MAKE_VERSION=4.4.1-r4
# renovate: datasource=repology depName=bash
ARG BASH_VERSION=5.3.9-r1
# renovate: datasource=repology depName=alpine-sdk
ARG ALPINE_SDK_VERSION=1.1-r1
# renovate: datasource=repology depName=protobuf
ARG PROTOBUF_VERSION=31.1-r1
# renovate: datasource=repology depName=protobuf-dev
ARG PROTOBUF_DEV_VERSION=31.1-r1
# renovate: datasource=repology depName=grpc
ARG GRPC_VERSION=1.78.1-r2
# renovate: datasource=repology depName=grpc-plugins
ARG GRPC_PLUGINS_VERSION=1.78.1-r2

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang22="${CLANG22_VERSION}" \
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
    && ./dotnet-install.sh -v 10.0.400 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
