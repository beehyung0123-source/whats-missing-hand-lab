$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$packageRoot = Join-Path $projectRoot '전시용_개선버전'
$zipPath = Join-Path $projectRoot '전시용_개선버전.zip'
if ((Test-Path -LiteralPath $packageRoot) -or (Test-Path -LiteralPath $zipPath)) {
    throw 'The improved exhibition package already exists. Inspect it before rebuilding.'
}
New-Item -ItemType Directory -Path $packageRoot | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$source = Join-Path $PSScriptRoot 'OnlineLauncher.cs'
foreach ($mode in @('start', 'stop')) {
    $name = if ($mode -eq 'start') { '전시시작.exe' } else { '전시종료.exe' }
    $options = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/codepage:65001',
        '/reference:System.Windows.Forms.dll', "/out:$(Join-Path $packageRoot $name)")
    if ($mode -eq 'stop') { $options += '/define:STOP_EXHIBIT' }
    & $compiler @options $source
    if ($LASTEXITCODE -ne 0) { throw "Could not build $name" }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '사용 안내.txt') -Destination $packageRoot
Compress-Archive -LiteralPath $packageRoot -DestinationPath $zipPath -CompressionLevel Optimal
Get-Item -LiteralPath $zipPath | Select-Object FullName, Length
