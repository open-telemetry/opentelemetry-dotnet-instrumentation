FROM mcr.microsoft.com/dotnet/sdk:7.0.305-alpine3.17

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=15.0.7-r0 \
        cmake=3.24.4-r0 \
        make=4.3-r1 \
        bash=5.2.15-r0 \
        alpine-sdk=1.0-r1 \
        protobuf=3.21.9-r0 \
        protobuf-dev=3.21.9-r0 \
        grpc=1.50.1-r0 \
        grpc-plugins=1.50.1-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "5840ce64f4186ccc4dac0c0fd8703acd0d387091ce48f310fef758e5f84d7a7f  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.410 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
