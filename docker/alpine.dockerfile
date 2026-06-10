FROM mcr.microsoft.com/dotnet/sdk:10.0.301-alpine3.23@sha256:fac7cce841f78faa4bca416fb4c636d1a129c09abd9b50e9b45664b95fd008a0
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang21=21.1.2-r2 \
        cmake=4.1.3-r0 \
        make=4.4.1-r3 \
        bash=5.3.3-r1 \
        alpine-sdk=1.1-r0 \
        protobuf=31.1-r1 \
        protobuf-dev=31.1-r1 \
        grpc=1.76.0-r2 \
        grpc-plugins=1.76.0-r2

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.314 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.421 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
