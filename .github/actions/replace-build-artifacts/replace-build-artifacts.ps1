$ErrorActionPreference = 'Stop'

$workspaceRoot = [System.IO.Path]::GetFullPath((Get-Location).Path)
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot 'bin/ci-artifacts/replacements'))
$targetRoot = [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot $env:TARGET_ROOT))

function Test-IsUnderRoot {
  param(
    [string] $Path,
    [string] $Root
  )

  $normalizedRoot = [System.IO.Path]::TrimEndingDirectorySeparator($Root)
  $normalizedPath = [System.IO.Path]::TrimEndingDirectorySeparator($Path)

  return $normalizedPath -eq $normalizedRoot -or $normalizedPath.StartsWith($normalizedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-SafeFullPath {
  param(
    [string] $Root,
    [string] $RelativePath
  )

  if ([string]::IsNullOrWhiteSpace($RelativePath)) {
    throw 'Replacement path must not be empty.'
  }

  if ($RelativePath -match '^[\\/]' -or $RelativePath -match '^[A-Za-z]:') {
    throw "Replacement path '$RelativePath' must be relative."
  }

  $segments = $RelativePath -split '[\\/]+' | Where-Object { $_ -ne '' }
  if ($segments.Count -eq 0) {
    throw "Replacement path '$RelativePath' must not be empty."
  }

  foreach ($segment in $segments) {
    if ($segment -eq '.' -or $segment -eq '..') {
      throw "Replacement path '$RelativePath' must not contain '.' or '..' segments."
    }
  }

  $combinedPath = [System.IO.Path]::GetFullPath((Join-Path $Root $RelativePath))
  if (-not (Test-IsUnderRoot -Path $combinedPath -Root $Root)) {
    throw "Replacement path '$RelativePath' escapes root '$Root'."
  }

  return $combinedPath
}

function Get-ReplacementFileList {
  param(
    [string] $Path,
    [string] $DisplayRoot
  )

  if (-not (Test-Path -LiteralPath $Path)) {
    return @()
  }

  $item = Get-Item -LiteralPath $Path -Force
  if ($item.PSIsContainer) {
    $items = Get-ChildItem -LiteralPath $Path -File -Recurse | ForEach-Object {
      $relativePath = [System.IO.Path]::GetRelativePath($Path, $_.FullName)
      ($DisplayRoot.TrimEnd('/', '\') + '/' + $relativePath.Replace('\', '/')).TrimStart('/')
    }

    return @($items | Sort-Object -Unique)
  }

  return @($DisplayRoot.Replace('\', '/'))
}

$replacementEntries = ($env:REPLACE_ARTIFACTS ?? '') -split '\r?\n'

foreach ($rawEntry in $replacementEntries) {
  $replacement = $rawEntry.Trim()
  if ([string]::IsNullOrWhiteSpace($replacement)) {
    continue
  }

  $separatorIndex = $replacement.IndexOf('!')
  if ($separatorIndex -lt 1 -or $separatorIndex -eq ($replacement.Length - 1)) {
    throw "Invalid replacement entry '$replacement'. Expected <artifact_name>!<path>."
  }

  $artifactName = $replacement.Substring(0, $separatorIndex).Trim()
  $replacePath = $replacement.Substring($separatorIndex + 1).Trim()

  if ($artifactName -match '[\\/:*?"<>|]') {
    throw "Artifact name '$artifactName' contains invalid path characters."
  }

  $artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $sourceRoot $artifactName))
  if (-not (Test-IsUnderRoot -Path $artifactRoot -Root $sourceRoot)) {
    throw "Artifact name '$artifactName' resolves outside '$sourceRoot'."
  }

  $sourcePath = Get-SafeFullPath -Root $artifactRoot -RelativePath $replacePath
  $targetPath = Get-SafeFullPath -Root $targetRoot -RelativePath $replacePath

  Write-Output "::group::Replace $replacePath from $artifactName"

  if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Replacement source '$sourcePath' does not exist."
  }

  $deletedFiles = Get-ReplacementFileList -Path $targetPath -DisplayRoot $replacePath
  $addedFiles = Get-ReplacementFileList -Path $sourcePath -DisplayRoot $replacePath

  $targetParent = Split-Path -Parent $targetPath
  if (-not [string]::IsNullOrWhiteSpace($targetParent)) {
    New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
  }

  if (Test-Path -LiteralPath $targetPath) {
    Remove-Item -LiteralPath $targetPath -Recurse -Force
  }

  Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Recurse -Force

  if ($deletedFiles.Count -gt 0) {
    Write-Output 'Deleted files:'
    $deletedFiles | ForEach-Object { Write-Output "  $_" }
  }
  else {
    Write-Output 'Deleted files: (none)'
  }

  if ($addedFiles.Count -gt 0) {
    Write-Output 'Added files:'
    $addedFiles | ForEach-Object { Write-Output "  $_" }
  }
  else {
    Write-Output 'Added files: (none)'
  }

  $deletedOnly = @($deletedFiles | Where-Object { $_ -notin $addedFiles })
  $addedOnly = @($addedFiles | Where-Object { $_ -notin $deletedFiles })

  if ($deletedOnly.Count -gt 0 -or $addedOnly.Count -gt 0) {
    $warningDetails = New-Object System.Collections.Generic.List[string]
    $warningDetails.Add("Replacement '$replacement' changed the file list.")

    if ($deletedOnly.Count -gt 0) {
      $warningDetails.Add('Deleted but not added: ' + ($deletedOnly -join ', '))
    }

    if ($addedOnly.Count -gt 0) {
      $warningDetails.Add('Added but not deleted: ' + ($addedOnly -join ', '))
    }

    Write-Output "::warning::$($warningDetails -join ' ')"
  }

  Write-Output "::endgroup::"
}
