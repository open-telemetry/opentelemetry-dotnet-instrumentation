#!/bin/bash
root=$(PWD)
loaderFolder=$root/src/Datadog.Trace.ClrProfiler.Managed.Loader
nativeFolder=$root/src/Datadog.Trace.ClrProfiler.Native
managedFolder=$root/src/Datadog.Trace.ClrProfiler.Managed
#destination `tracer-home` folder
destinationFolder=$root/deploy/macos
configuration=Debug

# Compile loader
echo Building loader...
cd $loaderFolder
dotnet build -c $configuration

# Compile native
echo Building native profiler...
cd $nativeFolder
#sudo rm -r -f build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=$configuration
make

# Compile managed libraries
echo Building managed libraries...
cd $managedFolder
dotnet build -c $configuration

# Copying artifacts to destination
echo Copying artifacts to destination folder
mkdir -p $destinationFolder
mkdir -p $destinationFolder/netstandard2.0
mkdir -p $destinationFolder/netcoreapp3.1
cp $root/integrations.json $destinationFolder/.
cp $nativeFolder/build/bin/Datadog.Trace.ClrProfiler.Native.dylib $destinationFolder/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dylib
cp -R $managedFolder/bin/$configuration/netcoreapp3.1/* $destinationFolder/netcoreapp3.1/.
cp -R $managedFolder/bin/$configuration/netstandard2.0/* $destinationFolder/netstandard2.0/.
