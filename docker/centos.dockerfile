FROM ghcr.io/open-telemetry/opentelemetry-dotnet-instrumentation-centos7-build-image:main@sha256:84cdf59d27df38a84dc63f0e9fbb309651a778e5106e3f7e2aa79b7665a4832b

RUN rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
RUN yum -y install dotnet-sdk-6.0-6.0.427-1 dotnet-sdk-7.0-7.0.410-1

ENV IsCentos=true
WORKDIR /project
COPY ./docker-entrypoint.sh /
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["/bin/bash"]
