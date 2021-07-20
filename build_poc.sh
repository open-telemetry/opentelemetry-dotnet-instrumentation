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
OUTDIR="$( pwd )/src/Datadog.Trace.ClrProfiler.Native/bin/${BUILD_TYPE}/x64"

# build Loader
dotnet build -c $BUILD_TYPE src/Datadog.Trace.ClrProfiler.Managed.Loader/Datadog.Trace.ClrProfiler.Managed.Loader.csproj

# build Native
os=$(uname_os)
case "$os" in
 windows*)
    SDK_TARGET_FRAMEWORKS="net452 net461 netstandard2.0 netcoreapp3.1"
    nuget restore "src\Datadog.Trace.ClrProfiler.Native\Datadog.Trace.ClrProfiler.Native.vcxproj" -SolutionDirectory .
    msbuild.exe Datadog.Trace.proj -t:BuildCpp -p:Configuration=${BUILD_TYPE} -p:Platform=x64
    ;;

 *)
    SDK_TARGET_FRAMEWORKS="netstandard2.0 netcoreapp3.1"
    cd src/Datadog.Trace.ClrProfiler.Native

    mkdir -p build
    (cd build && cmake ../ -DCMAKE_BUILD_TYPE=${BUILD_TYPE} && make)

    cd $DIR
    SUFIX=$(native_sufix)
    mkdir -p ${OUTDIR}
    cp -f src/Datadog.Trace.ClrProfiler.Native/build/bin/Datadog.Trace.ClrProfiler.Native.${SUFIX} ${OUTDIR}/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.${SUFIX}
esac

# build Managed
cd $DIR

for framework in ${SDK_TARGET_FRAMEWORKS} ; do
    mkdir -p "$OUTDIR/$framework"
    dotnet publish -f $framework -c ${BUILD_TYPE} src/OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed/OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.csproj -o "$OUTDIR/$framework"
    dotnet publish -f $framework -c ${BUILD_TYPE} samples/Vendor.Distro/Vendor.Distro.csproj -o "$OUTDIR/$framework"
done
