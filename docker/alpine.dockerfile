FROM mcr.microsoft.com/dotnet/sdk:8.0.303-alpine3.19
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=17.0.5-r0 \
        cmake=3.27.8-r0 \
        make=4.4.1-r2 \
        bash=5.2.21-r0 \
        alpine-sdk=1.0-r1 \
        protobuf=24.4-r0 \
        protobuf-dev=24.4-r0 \
        grpc=1.59.3-r0 \
        grpc-plugins=1.59.3-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "fe864d126da4d20b353e3983f186894a63009ce6fbe3108b0dac35a346331808  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.423 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.410 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
