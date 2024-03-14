FROM ghcr.io/open-telemetry/opentelemetry-dotnet-instrumentation-centos7-build-image:main

RUN rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
RUN yum -y install dotnet-sdk-6.0-6.0.420-1 dotnet-sdk-7.0-7.0.407-1

ENV IsCentos=true
WORKDIR /project
COPY ./docker-entrypoint.sh /
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["/bin/bash"]
