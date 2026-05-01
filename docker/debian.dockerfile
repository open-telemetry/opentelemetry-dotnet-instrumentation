FROM mcr.microsoft.com/dotnet/sdk:9.0.313-bookworm-slim@sha256:e04aef5e32fe25cea4641ecd2f2d8d7eca009f5d937bd545250f5ee5f35cf3b5
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via apt-get

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-10.0 \
        dotnet-sdk-8.0 \
        cmake \
        clang \
        make

WORKDIR /project
