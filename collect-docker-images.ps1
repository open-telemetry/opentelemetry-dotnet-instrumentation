# Helper script to collect data about docker images cached locally

function Format-Bytes {
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [ValidateNotNullOrEmpty()]
        [int64]$Bytes
    )

    $suffixes = "B", "KB", "MB", "GB", "TB"
    $index = 0

    while ($Bytes -ge 1024 -and $index -lt $suffixes.Count) {
        $Bytes = $Bytes / 1024
        $index++
    }

    "{0:N2} {1}" -f $Bytes, $suffixes[$index]
}

function New-Folder {
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [string]$folderName
    )
    if (-not (Test-Path -Path $folderName -PathType Container)) {
        New-Item -ItemType Directory -Path $folderName | Out-Null
    } else {
        Remove-Item -Recurse -Force "$folderName\*"
    }
}

function Get-DockerImages {
    param(
        [Parameter(Mandatory=$true)]
        [string]$filter,
        [Parameter(Mandatory=$true)]
        [string]$outputFolder
    )

    New-Folder $outputFolder
    docker image ls -q --filter $filter | Select-Object -First 2 | ForEach-Object { 
        Start-Process docker -Argument "save -o $("$outputFolder\$_.tar") $_" -PassThru -NoNewWindow | Wait-Process
    }
    $files = Get-ChildItem "$outputFolder\*.tar" -Recurse -File
    $size = ($files | Measure-Object -Property Length -Sum).Sum
    Write-Host "`n==========================`nImages size in $outputFolder $(Format-Bytes $size)`n==========================`n"
}

Write-Host "`n---------------------------------------------------------`nContainers:"
docker container ls | Sort-Object
Write-Host "---------------------------------------------------------`n"

Write-Host "`n---------------------------------------------------------`nNon-dangling images:"
docker image ls --format "{{.Repository}}:{{.Tag}}" --filter dangling=false | Sort-Object
Write-Host "---------------------------------------------------------`n"

Write-Host "`n---------------------------------------------------------`nDangling images:"
docker image ls --format "{{.Repository}}:{{.Tag}}" --filter dangling=true | Sort-Object
Write-Host "---------------------------------------------------------`n"

Get-DockerImages "dangling=false"  ".\non-dangling-images"
Get-DockerImages "dangling=true"   ".\dangling-images"
