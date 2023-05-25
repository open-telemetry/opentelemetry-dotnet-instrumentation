function Test-AssemblyNotForGAC([string] $Name) {
    switch ($Name) {
        "netstandard.dll" { return $true }
        "grpc_csharp_ext.x64.dll" { return $true }
        "grpc_csharp_ext.x86.dll" { return $true }
    }
    return $false 
}

[System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a") | Out-Null
$publish = New-Object System.EnterpriseServices.Internal.Publish 
$dlls = Get-ChildItem -Path C:\opentelemetry\netfx\ -Filter *.dll -File
for ($i = 0; $i -lt $dlls.Count; $i++) {
    if (Test-AssemblyNotForGAC $dlls[$i].Name) {
        continue
    }

    $publish.GacInstall($dlls[$i].FullName)
}
Write-Progress -Activity "Registering .NET Framework dlls in GAC" -Status "Ready" -Completed
