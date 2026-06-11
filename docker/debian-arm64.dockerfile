FROM debian:bookworm-slim@sha256:0104b334637a5f19aa9c983a91b54c89887c0984081f2068983107a6f6c21eeb

RUN apt-get update && \
    apt-get install -y \
        bash \
        ca-certificates \
        clang \
        cmake \
        curl \
        git \
        libgssapi-krb5-2 \
        libicu72 \
        libssl3 \
        libstdc++6 \
        make \
        zlib1g

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 11.0.100-preview.5.26302.115 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 10.0.301 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

WORKDIR /project
