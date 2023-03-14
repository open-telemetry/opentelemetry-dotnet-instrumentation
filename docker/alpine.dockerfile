FROM mcr.microsoft.com/dotnet/sdk:7.0.201-alpine3.16

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=13.0.1-r1 \
        cmake=3.23.1-r0 \
        make=4.3-r0 \
        bash=5.1.16-r2 \
        alpine-sdk=1.0-r1 \
        protobuf=3.18.1-r3 \
        protobuf-dev=3.18.1-r3 \
        grpc=1.46.3-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "3d5a87bc29fb96e8dac8c2f88d95ff619c3a921903b4c9ff720e07ca0906d55e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.406 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
