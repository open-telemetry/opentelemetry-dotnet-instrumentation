FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.16

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang \
        cmake \
        make \
        bash \
        alpine-sdk \
        protobuf \
        protobuf-dev \
        grpc

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
   && chmod +x ./dotnet-install.sh \
   && ./dotnet-install.sh -c 6.0 --install-dir /usr/share/dotnet --no-path \
   && rm dotnet-install.sh

WORKDIR /project
