FROM mcr.microsoft.com/dotnet/sdk:7.0.101-bullseye-slim-arm64v8

RUN echo "deb http://deb.debian.org/debian stretch-backports main" > /etc/apt/sources.list.d/backports.list

RUN apt-get update \
    && apt-get upgrade \
    && apt-get install -y \
        curl \
        clang \
        cmake \
        make \
        protobuf-compiler

RUN apt-get install -y -t stretch-backports \
        libgrpc++-dev \
        libgrpc++1 \
        libgrpc6 \
        libgrpc-dev \
        protobuf-compiler-grpc 

ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
   && chmod +x ./dotnet-install.sh \
   && ./dotnet-install.sh -c 6.0 --install-dir /usr/share/dotnet --no-path \
   && rm dotnet-install.sh

WORKDIR /project
