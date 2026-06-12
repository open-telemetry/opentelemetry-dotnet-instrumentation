FROM mcr.microsoft.com/dotnet/sdk:9.0.315-bookworm-slim@sha256:26bd075f66699a1fc32f1ce95b78cdae878b2760053823ede81c8ed49ceadc19
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.301 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.422 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
