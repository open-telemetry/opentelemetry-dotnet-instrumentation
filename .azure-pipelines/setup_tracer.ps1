$ProgressPreference = 'SilentlyContinue'

echo "Getting latest release version"
# Get the latest release tag from the github release page
$release_version = (Invoke-WebRequest https://api.github.com/repos/datadog/dd-trace-dotnet/releases | ConvertFrom-Json)[0].tag_name.SubString(1)

$otel_tracer_workingfolder = $env:OTEL_TRACER_WORKINGFOLDER
$otel_tracer_home = ""
$otel_tracer_msbuild = ""
$otel_tracer_integrations = ""
$otel_tracer_profiler_32 = ""
$otel_tracer_profiler_64 = ""


# Download the binary file depending of the current operating system and extract the content to the "release" folder 
echo "Downloading tracer v$release_version"
if ($env:os -eq "Windows_NT") 
{
    $url = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$($release_version)/windows-tracer-home.zip"

    Invoke-WebRequest -Uri $url -OutFile windows.zip
    echo "Extracting windows.zip"
    Expand-Archive windows.zip -DestinationPath .\release
    Remove-Item windows.zip

    if ([string]::IsNullOrEmpty($otel_tracer_workingfolder)) {
        $otel_tracer_home = "$(pwd)\release"
    } else {
        $otel_tracer_home = "$otel_tracer_workingfolder\release"
    }

    $otel_tracer_msbuild = "$otel_tracer_home\netstandard2.0\OpenTelemetry.AutoInstrumentation.MSBuild.dll"
    $otel_tracer_integrations = "$otel_tracer_home\integrations.json"
    $otel_tracer_profiler_32 = "$otel_tracer_home\win-x86\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"
    $otel_tracer_profiler_64 = "$otel_tracer_home\win-x64\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"
} 
else 
{
    # File version is the same as the release version without the prerelease suffix.
    $file_version = $release_version.replace("-prerelease", "")

    $url = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$($release_version)/datadog-dotnet-apm-$($file_version).tar.gz"

    Invoke-WebRequest -Uri $url -OutFile linux.tar.gz
    mkdir release
    echo "Extracting linux.tar.gz"
    tar -xvzf linux.tar.gz -C ./release
    Remove-Item linux.tar.gz
    # Ensure the profiler can write the native log profiler
    sudo mkdir -p /var/log/datadog/dotnet
    sudo chmod -R 777 /var/log/datadog/dotnet
    
    if ([string]::IsNullOrEmpty($otel_tracer_workingfolder)) {
        $otel_tracer_home = "$(pwd)/release"
    } else {
        $otel_tracer_home = "$otel_tracer_workingfolder/release"
    }

    $otel_tracer_msbuild = "$otel_tracer_home/netstandard2.0/OpenTelemetry.AutoInstrumentation.MSBuild.dll"
    $otel_tracer_integrations = "$otel_tracer_home/integrations.json"
    $otel_tracer_profiler_64 = "$otel_tracer_home/Datadog.Trace.ClrProfiler.Native.so"
}

# Set all environment variables to attach the profiler to the following pipeline steps
echo "Setting environment variables..."

echo "##vso[task.setvariable variable=OTEL_ENV]CI"
echo "##vso[task.setvariable variable=OTEL_DOTNET_TRACER_HOME]$otel_tracer_home"
echo "##vso[task.setvariable variable=OTEL_DOTNET_TRACER_MSBUILD]$otel_tracer_msbuild"
echo "##vso[task.setvariable variable=OTEL_INTEGRATIONS]$otel_tracer_integrations"

echo "##vso[task.setvariable variable=CORECLR_ENABLE_PROFILING]1"
echo "##vso[task.setvariable variable=CORECLR_PROFILER]{918728DD-259F-4A6A-AC2B-B85E1B658318}"
echo "##vso[task.setvariable variable=CORECLR_PROFILER_PATH_32]$otel_tracer_profiler_32"
echo "##vso[task.setvariable variable=CORECLR_PROFILER_PATH_64]$otel_tracer_profiler_64"

echo "##vso[task.setvariable variable=COR_ENABLE_PROFILING]1"
echo "##vso[task.setvariable variable=COR_PROFILER]{918728DD-259F-4A6A-AC2B-B85E1B658318}"
echo "##vso[task.setvariable variable=COR_PROFILER_PATH_32]$otel_tracer_profiler_32"
echo "##vso[task.setvariable variable=COR_PROFILER_PATH_64]$otel_tracer_profiler_64"

echo "Done."