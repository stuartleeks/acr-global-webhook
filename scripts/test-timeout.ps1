#
# Only send one of the expected notifications to trigger a timeout
#
param(
    [string] $FunctionUrl = "http://localhost:7071/api/ImagePush",
    [string] $ImageName = "nginx",
    [string] $ImageTag = "timeout"
)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$hookid = (New-Guid).Guid

Write-Host "Sending trigger for northeurope..."
&"$here/invoke-trigger.ps1" -Region "northeurope" -NotificationId $hookid -ImageName $ImageName -ImageTag $ImageTag

Write-Host "Done"