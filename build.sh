#!/bin/bash
set -euxo pipefail

uname_os() {
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    case "$os" in
        cygwin_nt*) echo "windows" ;;
        mingw*) echo "windows" ;;
        msys_nt*) echo "windows" ;;
        *) echo "$os" ;;
    esac
}

native_sufix() {
    os=$(uname_os)
    case "$os" in
        windows*) echo "dll" ;;
        linux*) echo "so" ;;
        darwin*) echo "dylib" ;;
        *) echo "OS: ${os} is not supported" ; exit 1 ;;
    esac
}

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
cd $DIR

BUILD_TYPE=${buildConfiguration:-Debug}

# build Loader
dotnet build -c $BUILD_TYPE src/Datadog.Trace.ClrProfiler.Managed.Loader/Datadog.Trace.ClrProfiler.Managed.Loader.csproj

# build Native
os=$(uname_os)
case "$os" in
 windows*)
    nuget restore "src\Datadog.Trace.ClrProfiler.Native\Datadog.Trace.ClrProfiler.Native.vcxproj" -SolutionDirectory .
    msbuild.exe Datadog.Trace.proj -t:BuildCpp -p:Configuration=${BUILD_TYPE} -p:Platform=x64
    ;;

 *)
    OUTDIR="$( pwd )/src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64"
    cd src/Datadog.Trace.ClrProfiler.Native

    mkdir -p build
    (cd build && cmake ../ -DCMAKE_BUILD_TYPE=${BUILD_TYPE} && make)

    OUTDIR="bin/${BUILD_TYPE}/x64"
    SUFIX=$(native_sufix)
    mkdir -p ${OUTDIR}
    cp -f build/bin/Datadog.Trace.ClrProfiler.Native.${SUFIX} ${OUTDIR}/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.${SUFIX}
esac
