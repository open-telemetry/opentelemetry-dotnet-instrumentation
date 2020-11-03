FROM mcr.microsoft.com/dotnet/core/sdk:3.1

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
