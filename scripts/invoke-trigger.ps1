param(
    [string] $FunctionUrl = "http://localhost:7071/api/ImagePush",
    [string] $ImageName = "nginx",
    [string] $ImageTag = "latest",
    [Parameter(Mandatory = $true)]
    [string] $Region,
    [Parameter(Mandatory = $true)]
    [string] $NotificationId    
)

Invoke-RestMethod -uri "http://localhost:7071/api/ImagePush?region=$Region" -Body "{
        ""id"": ""$NotificationId"",
        ""timestamp"": ""2017-12-14T20:43:46.689658959Z"",
        ""action"": ""push"",
    ""target"": {
        ""mediaType"": ""application/vnd.docker.distribution.manifest.v2+json"",
        ""size"": 948,
        ""digest"": ""sha256:3eff18554e47c4177a09cea5d460526cbb4d3aff9fd1917d7b1372da1539694a"",
        ""length"": 948,
        ""repository"": ""$ImageName"",
        ""tag"": ""$ImageTag""
    },
    ""request"": {
        ""id"": ""d6c9fbf2-b5c5-4172-b592-dbdd2fe3f7ca"",
        ""host"": ""acrhooks.azurecr.io"",
      ""method"": ""PUT"",
      ""useragent"": ""docker/17.09.0-ce go/go1.8.3 git-commit/afdb6d4 kernel/4.4.0-97-generic os/linux arch/amd64 UpstreamClient(Docker-Client/17.09.0-ce \\(linux\\))""
    }
  }" -Method post -ContentType "application/json"    
