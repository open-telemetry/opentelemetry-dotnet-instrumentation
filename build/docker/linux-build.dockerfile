# This is a multistage docker file, used by the linux-build.bash file to create the two images from two different stages
# The 'tracer-build' stage contains only the result of building the tracer (managed+native) without any other files (from scratch)
# The 'dotnet-sdk-with-dd-tracer' stage contains the dotnet sdk 3.1 as a base image.
# Also the dockerfile contains arguments to customize the build process.

ARG BUILD_CONFIGURATION=Release
ARG WORKSPACE=/workspace
ARG PUBLISH_FOLDER=/workspace/publish
ARG TRACER_HOME=/dd-tracer-dotnet

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-managed-base
# Instructions to install .NET Core runtimes from
# https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-debian10
RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg && \
    mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ && \
    wget -q https://packages.microsoft.com/config/debian/10/prod.list && \
    mv prod.list /etc/apt/sources.list.d/microsoft-prod.list && \
    chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg && \
    chown root:root /etc/apt/sources.list.d/microsoft-prod.list
RUN apt-get update && \
    apt-get install -y apt-transport-https && \
    apt-get update && \
    apt-get install -y aspnetcore-runtime-2.1 && \
    apt-get install -y aspnetcore-runtime-3.0
ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /bin/wait-for-it
RUN chmod +x /bin/wait-for-it



FROM ubuntu:14.04 AS build-native-base
RUN apt-get update && \
    apt-get install -y \
        git \
        wget
RUN echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.9 main" | sudo tee /etc/apt/sources.list.d/llvm.list
RUN wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
RUN sudo apt-get update
RUN sudo apt-get install -y \
    cmake \
    llvm-3.9 \
    clang-3.9 \
    lldb-3.9 \
    liblldb-3.9-dev \
    libunwind8 \
    libunwind8-dev \
    gettext \
    libicu-dev \
    liblttng-ust-dev \
    libcurl4-openssl-dev \
    libssl-dev \
    libnuma-dev \
    libkrb5-dev
RUN cd /usr/lib/llvm-3.9/lib && ln -s ../../x86_64-linux-gnu/liblldb-3.9.so.1 liblldb-3.9.so.1
RUN apt-get update && apt-get install -y \
    python-software-properties \
    software-properties-common
RUN add-apt-repository ppa:ubuntu-toolchain-r/test && \
    apt-get update && \
    apt-get install -y \
        curl \
        ninja-build
# cmake
RUN apt-get remove -y cmake && \
    curl -o /tmp/cmake.sh https://cmake.org/files/v3.12/cmake-3.12.3-Linux-x86_64.sh && \
    sh /tmp/cmake.sh --prefix=/usr/local --exclude-subdir --skip-license

# libraries
RUN mkdir -p /opt
ENV CXX=clang++-3.9
ENV CC=clang-3.9

# - nlohmann/json
RUN cd /opt && git clone --depth 1 --branch v3.3.0 https://github.com/nlohmann/json.git
# RUN cd /opt/json && cmake -G Ninja . && cmake --build .

# - re2
RUN cd /opt && git clone --depth 1 --branch 2018-10-01 https://github.com/google/re2.git
RUN cd /opt/re2 && env CXXFLAGS="-O3 -g -fPIC" make




FROM build-managed-base as build-managed
ARG BUILD_CONFIGURATION
ARG WORKSPACE
ARG PUBLISH_FOLDER
WORKDIR ${WORKSPACE}
COPY . ./

RUN mkdir -p "${PUBLISH_FOLDER}"
RUN cp ./integrations.json ${PUBLISH_FOLDER}/
RUN dotnet build -c ${BUILD_CONFIGURATION} src/Datadog.Trace.ClrProfiler.Managed.Loader/Datadog.Trace.ClrProfiler.Managed.Loader.csproj

RUN mkdir -p "${PUBLISH_FOLDER}/netstandard2.0"
RUN dotnet publish -f netstandard2.0 -c ${BUILD_CONFIGURATION} src/Datadog.Trace/Datadog.Trace.csproj && \
    dotnet publish -f netstandard2.0 -c ${BUILD_CONFIGURATION} src/Datadog.Trace.OpenTracing/Datadog.Trace.OpenTracing.csproj && \
    dotnet publish -f netstandard2.0 -c ${BUILD_CONFIGURATION} src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj -o "${PUBLISH_FOLDER}/netstandard2.0"



FROM build-native-base as build-native
ARG WORKSPACE
ARG PUBLISH_FOLDER
ARG TRACER_HOME
COPY --from=build-managed ${WORKSPACE} ${WORKSPACE}
WORKDIR ${WORKSPACE}/src/Datadog.Trace.ClrProfiler.Native/obj/Debug/x64
RUN cmake ../../.. && make && cp -f Datadog.Trace.ClrProfiler.Native.so ${PUBLISH_FOLDER}/
RUN mkdir -p /var/log/datadog/dotnet
RUN touch /var/log/datadog/dotnet/dotnet-profiler.log
WORKDIR ${PUBLISH_FOLDER}
RUN echo "#!/bin/bash\n set -euxo pipefail\n export CORECLR_ENABLE_PROFILING=\"1\"\n export CORECLR_PROFILER=\"{846F5F1C-F9AE-4B07-969E-05C26BC060D8}\"\n export DD_DOTNET_TRACER_HOME=\"${TRACER_HOME}\"\n export CORECLR_PROFILER_PATH=\"\${DD_DOTNET_TRACER_HOME}/Datadog.Trace.ClrProfiler.Native.so\"\n export DD_INTEGRATIONS=\"\${DD_DOTNET_TRACER_HOME}/integrations.json\"\n eval \"\$@\"\n" > dd-trace.bash
RUN chmod +x dd-trace.bash


FROM scratch as tracer-build
ARG PUBLISH_FOLDER
ARG TRACER_HOME
COPY --from=build-native ${PUBLISH_FOLDER} ${TRACER_HOME}
COPY --from=build-native /var/log/datadog/ /var/log/datadog/


FROM build-native as native-linux-binary
ARG PUBLISH_FOLDER
COPY --from=build-native ${PUBLISH_FOLDER}/Datadog.Trace.ClrProfiler.Native.so Datadog.Trace.ClrProfiler.Native.so
CMD cp -f Datadog.Trace.ClrProfiler.Native.so /home/linux-x64/Datadog.Trace.ClrProfiler.Native.so


FROM build-managed-base as dotnet-sdk-with-dd-tracer
ARG TRACER_HOME
COPY --from=tracer-build . ./
RUN ls ${TRACER_HOME}