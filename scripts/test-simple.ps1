#
# Test a simple set of notifications within the timeout period
#
param(
    [string] $FunctionUrl = "http://localhost:7071/api/ImagePush",
    [string] $ImageName = "nginx",
    [string] $ImageTag = "simple",
    [int] $SecondsToDelayBetweenTriggers = 10 # Make sure this is less than the WebhookTimeout setting
)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$hookid = (New-Guid).Guid

Write-Host "Sending trigger for northeurope..."
&"$here/invoke-trigger.ps1" -Region "northeurope" -NotificationId $hookid -ImageName $ImageName -ImageTag $ImageTag

Write-Host "Delaying for $SecondsToDelayBetweenTriggers seconds..."
Start-Sleep -Seconds $SecondsToDelayBetweenTriggers

Write-Host "Sending trigger for westeurope..."
&"$here/invoke-trigger.ps1" -Region "westeurope" -NotificationId $hookid -ImageName $ImageName -ImageTag $ImageTag

Write-Host "Done"