FROM mcr.microsoft.com/dotnet/sdk:10.0.102-alpine3.22@sha256:bd7f021a8293d1a21a51824b76b15e930c6024bd85bc78859c5f249cb07f3a7e
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=20.1.8-r0 \
        cmake=3.31.7-r1 \
        make=4.4.1-r3 \
        bash=5.2.37-r0 \
        alpine-sdk=1.1-r0 \
        protobuf=29.4-r0 \
        protobuf-dev=29.4-r0 \
        grpc=1.72.0-r0 \
        grpc-plugins=1.72.0-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.310 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.417 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
