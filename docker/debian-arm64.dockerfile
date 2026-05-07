FROM mcr.microsoft.com/dotnet/sdk:9.0.313-bookworm-slim@sha256:f9ddb8a31ae90f4b38d18355d82f03d76dcdd2a57d7235a2ffdf008fab11a862
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.203 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.420 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
