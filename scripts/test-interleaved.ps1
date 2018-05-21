#
# Test interleaved notifications for two separate sets of image updates
#
param(
    [string] $FunctionUrl = "http://localhost:7071/api/ImagePush",
    [string] $ImageName = "nginx",
    [string] $ImageTag1 = "tag1",
    [string] $ImageTag2 = "tag2",
    [int] $SecondsToDelayBetweenTriggers = 5 # Make sure this is less than the WebhookTimeout setting
)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$hookid1 = (New-Guid).Guid
$hookid2 = (New-Guid).Guid

Write-Host "Sending trigger for northeurope for notification 1..."
&"$here/invoke-trigger.ps1" -Region "northeurope" -NotificationId $hookid1 -ImageName $ImageName -ImageTag $ImageTag1

Write-Host "Delaying for $SecondsToDelayBetweenTriggers seconds..."
Start-Sleep -Seconds $SecondsToDelayBetweenTriggers

Write-Host "Sending trigger for westeurope for notification 2..."
&"$here/invoke-trigger.ps1" -Region "westeurope" -NotificationId $hookid2 -ImageName $ImageName -ImageTag $ImageTag2

Write-Host "Delaying for $SecondsToDelayBetweenTriggers seconds..."
Start-Sleep -Seconds $SecondsToDelayBetweenTriggers

Write-Host "Sending trigger for northeurope for notification 2..."
&"$here/invoke-trigger.ps1" -Region "northeurope" -NotificationId $hookid2 -ImageName $ImageName -ImageTag $ImageTag2

Write-Host "Delaying for $SecondsToDelayBetweenTriggers seconds..."
Start-Sleep -Seconds $SecondsToDelayBetweenTriggers

Write-Host "Sending trigger for westeurope for notification 1..."
&"$here/invoke-trigger.ps1" -Region "westeurope" -NotificationId $hookid1 -ImageName $ImageName -ImageTag $ImageTag1


Write-Host "Done"