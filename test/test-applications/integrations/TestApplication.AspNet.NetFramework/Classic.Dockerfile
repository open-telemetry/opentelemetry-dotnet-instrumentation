# escape=`

FROM testapplication-aspnet-netframework:latest
RUN Start-IISCommitDelay;`
    (Get-IISAppPool -Name DefaultAppPool).ManagedPipelineMode = 'Classic';`
    Stop-IISCommitDelay -Commit $True
