FROM debian:bookworm-slim@sha256:0104b334637a5f19aa9c983a91b54c89887c0984081f2068983107a6f6c21eeb

# renovate: datasource=deb depName=bash
ARG BASH_VERSION=5.2.15-2+b13
# renovate: datasource=deb depName=ca-certificates
ARG CA_CERTIFICATES_VERSION=20250419~deb12u2
# renovate: datasource=deb depName=clang
ARG CLANG_VERSION=1:14.0-55.7~deb12u1
# renovate: datasource=deb depName=cmake
ARG CMAKE_VERSION=3.25.1-1
# renovate: datasource=deb depName=curl
ARG CURL_VERSION=7.88.1-10+deb12u15
# renovate: datasource=deb depName=git
ARG GIT_VERSION=1:2.39.5-0+deb12u3
# renovate: datasource=deb depName=libgssapi-krb5-2
ARG LIBGSSAPI_KRB5_2_VERSION=1.20.1-2+deb12u5
# renovate: datasource=deb depName=libicu72
ARG LIBICU72_VERSION=72.1-3+deb12u1
# renovate: datasource=deb depName=libssl3
ARG LIBSSL3_VERSION=3.0.22-1~deb12u2
# renovate: datasource=deb depName=libstdc++6
ARG LIBSTDCXX6_VERSION=12.2.0-14+deb12u1
# renovate: datasource=deb depName=make
ARG MAKE_VERSION=4.3-4.1
# renovate: datasource=deb depName=zlib1g
ARG ZLIB1G_VERSION=1:1.2.13.dfsg-1

RUN apt-get update && \
    apt-get install -y \
        bash="${BASH_VERSION}" \
        ca-certificates="${CA_CERTIFICATES_VERSION}" \
        clang="${CLANG_VERSION}" \
        cmake="${CMAKE_VERSION}" \
        curl="${CURL_VERSION}" \
        git="${GIT_VERSION}" \
        libgssapi-krb5-2="${LIBGSSAPI_KRB5_2_VERSION}" \
        libicu72="${LIBICU72_VERSION}" \
        libssl3="${LIBSSL3_VERSION}" \
        libstdc++6="${LIBSTDCXX6_VERSION}" \
        make="${MAKE_VERSION}" \
        zlib1g="${ZLIB1G_VERSION}"

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 11.0.100 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 10.0.404 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

ENV PATH="$PATH:/usr/share/dotnet"

WORKDIR /project
