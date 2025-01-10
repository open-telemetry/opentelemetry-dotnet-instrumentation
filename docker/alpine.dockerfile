FROM mcr.microsoft.com/dotnet/sdk:9.0.101-alpine3.20@sha256:cdc618c61fb14b297986a06ea895efe6eb49e30dfe3b2b6c8b4793e600a5f298
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=17.0.6-r1 \
        cmake=3.29.3-r0 \
        make=4.4.1-r2 \
        bash=5.2.26-r0 \
        alpine-sdk=1.0-r1 \
        protobuf=24.4-r1 \
        protobuf-dev=24.4-r1 \
        grpc=1.62.1-r0 \
        grpc-plugins=1.62.1-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "48e5763854527aca84bf2c9b1542a22ec490e85657725eac8dc18eb0ed809413  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.404 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
