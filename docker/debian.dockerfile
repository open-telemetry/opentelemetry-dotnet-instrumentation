FROM mcr.microsoft.com/dotnet/sdk:9.0.300-bookworm-slim@sha256:90872f8e7f1fd2b93989b81fb7f152c3bef4fe817470a3227abaa18c873dba60

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-8.0 \
        cmake \
        clang \
        make

WORKDIR /project
