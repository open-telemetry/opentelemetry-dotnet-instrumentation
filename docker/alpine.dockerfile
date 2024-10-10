FROM mcr.microsoft.com/dotnet/sdk:8.0.403-alpine3.20
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
    && echo "8b33761700040a9cd7f835f181a7c350b866e42425540c3a1894cc7919b275e3  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.427 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.410 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
