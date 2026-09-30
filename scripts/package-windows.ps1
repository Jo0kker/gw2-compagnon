param([string]$Project = 'src/Companion.Desktop', [string]$Output = 'artifacts/windows-preview')
$ErrorActionPreference = 'Stop'
dotnet publish $Project --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=false --output $Output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item LICENSE, THIRD_PARTY_NOTICES.md, PRIVACY.md -Destination $Output
$licenses = New-Item -ItemType Directory -Force (Join-Path $Output 'licenses')
$dotnetRoot = Split-Path (Get-Command dotnet).Source
Copy-Item (Join-Path $dotnetRoot 'LICENSE.txt') (Join-Path $licenses.FullName 'dotnet-LICENSE.txt')
Copy-Item (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') (Join-Path $licenses.FullName 'dotnet-ThirdPartyNotices.txt')
