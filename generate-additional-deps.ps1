# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'

function Get-ExtendedFileSystemPath {
    param([string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($env:OS -ne 'Windows_NT' -or $fullPath.StartsWith('\\?\', [System.StringComparison]::Ordinal)) {
        return $fullPath
    }

    if ($fullPath.StartsWith('\\', [System.StringComparison]::Ordinal)) {
        return '\\?\UNC\' + $fullPath.Substring(2)
    }

    return '\\?\' + $fullPath
}

$copyPlanPath = Join-Path $PSScriptRoot 'additional-deps-copy-plan.txt'

if (-not (Test-Path -LiteralPath $copyPlanPath -PathType Leaf)) {
    throw "AdditionalDeps copy plan was not found: $copyPlanPath"
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$destinationAdditionalDeps = Join-Path $outputFullPath 'AdditionalDeps'
$destinationStore = Join-Path $outputFullPath 'store'

foreach ($line in Get-Content -LiteralPath $copyPlanPath) {
    $fields = @($line.Split('|'))
    if ($fields.Count -ne 2 -or $fields.Where({ [string]::IsNullOrWhiteSpace($_) }).Count -ne 0) {
        throw "Invalid entry in AdditionalDeps copy plan: $line"
    }

    $sourceRelativePath, $destinationRelativePath = $fields
    foreach ($path in $fields) {
        if ([System.IO.Path]::IsPathRooted($path) -or ($path -split '[\\/]').Contains('..')) {
            throw "AdditionalDeps copy plan contains an unsafe path: $line"
        }
    }

    $sourcePath = Join-Path $PSScriptRoot $sourceRelativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "AdditionalDeps source file was not found: $sourcePath"
    }

    $destinationPath = Join-Path $outputFullPath $destinationRelativePath
    $destinationDirectory = [System.IO.Path]::GetDirectoryName($destinationPath)
    [System.IO.Directory]::CreateDirectory((Get-ExtendedFileSystemPath $destinationDirectory)) | Out-Null
    [System.IO.File]::Copy(
        (Get-ExtendedFileSystemPath $sourcePath),
        (Get-ExtendedFileSystemPath $destinationPath),
        $true)
}

Write-Output 'AdditionalDeps files were generated successfully.'
Write-Output ''
Write-Output 'Configure the instrumented application with:'
Write-Output "DOTNET_ADDITIONAL_DEPS=$destinationAdditionalDeps"
Write-Output "DOTNET_SHARED_STORE=$destinationStore"
Write-Output ''
Write-Output 'If assembly redirection must be disabled, also configure:'
Write-Output 'OTEL_DOTNET_AUTO_REDIRECT_ENABLED=false'
