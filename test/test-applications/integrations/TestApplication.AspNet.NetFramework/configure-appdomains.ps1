$ErrorActionPreference = 'Stop'

Import-Module WebAdministration
New-Website -Name 'App B' -PhysicalPath 'C:\inetpub\app-b' -Port 80 -HostHeader 'app-b.local' -ApplicationPool 'DefaultAppPool'

$apps = @(
    @{ Config = 'C:\inetpub\wwwroot\Web.config'; EnableKey = 'OTEL_experimental_file_based_configuration_enabled'; FileKey = 'OTEL_config_file'; File = 'C:\inetpub\wwwroot\app-a.yaml' },
    @{ Config = 'C:\inetpub\app-b\Web.config'; EnableKey = 'OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED'; FileKey = 'OTEL_CONFIG_FILE'; File = 'app-b.yaml' }
)

foreach ($app in $apps) {
    [xml]$config = Get-Content -LiteralPath $app.Config -Raw
    foreach ($setting in @($config.configuration.appSettings.add | Where-Object { $_.key -like 'OTEL_*' })) {
        [void]$config.configuration.appSettings.RemoveChild($setting)
    }

    foreach ($entry in @(@{ Key = $app.EnableKey; Value = 'true' }, @{ Key = $app.FileKey; Value = $app.File })) {
        $setting = $config.CreateElement('add')
        $setting.SetAttribute('key', $entry.Key)
        $setting.SetAttribute('value', $entry.Value)
        [void]$config.configuration.appSettings.AppendChild($setting)
    }

    $config.Save($app.Config)
}
