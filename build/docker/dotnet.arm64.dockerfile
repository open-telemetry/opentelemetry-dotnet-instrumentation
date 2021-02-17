FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim

# Install aspnetcore-runtime-3.1.10
RUN wget https://download.visualstudio.microsoft.com/download/pr/936a9563-1dad-4c4b-b366-c7fcc3e28215/a1edcaf4c35bce760d07e3f1f3d0b9cf/aspnetcore-runtime-3.1.10-linux-arm64.tar.gz && \
    tar zxf aspnetcore-runtime-3.1.10-linux-arm64.tar.gz -C "/usr/share/dotnet"

# Install aspnetcore-runtime-5.0.1
RUN wget https://download.visualstudio.microsoft.com/download/pr/e12f9b23-cb47-4718-9903-8a000f85a442/d1a6a6c75cc832ad8187f5bce0d6234a/aspnetcore-runtime-5.0.1-linux-arm64.tar.gz && \
    tar zxf aspnetcore-runtime-5.0.1-linux-arm64.tar.gz -C "/usr/share/dotnet"

# Install Docker CE CLI
RUN apt-get update \
    && apt-get install -y apt-transport-https ca-certificates curl gnupg2 lsb-release \
    && curl -fsSL https://download.docker.com/linux/$(lsb_release -is | tr '[:upper:]' '[:lower:]')/gpg | apt-key add - 2>/dev/null \
    && echo "deb [arch=amd64] https://download.docker.com/linux/$(lsb_release -is | tr '[:upper:]' '[:lower:]') $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list \
    && apt-get update \
    && apt-get install -y docker-ce-cli 

# Install Docker Compose
RUN export LATEST_COMPOSE_VERSION=$(curl -sSL "https://api.github.com/repos/docker/compose/releases/latest" | grep -o -P '(?<="tag_name": ").+(?=")') \
    && curl -sSL "https://github.com/docker/compose/releases/download/${LATEST_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose \
    && chmod +x /usr/local/bin/docker-compose

ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /bin/wait-for-it
RUN chmod +x /bin/wait-for-it 