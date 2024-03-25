FROM mcr.microsoft.com/dotnet/sdk:8.0.203-alpine3.18
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=16.0.6-r1 \
        cmake=3.26.5-r0 \
        make=4.4.1-r1 \
        bash=5.2.15-r5 \
        alpine-sdk=1.0-r1 \
        protobuf=3.21.12-r2 \
        protobuf-dev=3.21.12-r2 \
        grpc=1.54.2-r0 \
        grpc-plugins=1.54.2-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "5eb82d8578f55cdadcb2edfd35ec649a2c6fc11a682e876b1cd68077badbf794  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.420 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.407 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
