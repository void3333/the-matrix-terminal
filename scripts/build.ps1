param([string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root 'dist' }
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { throw 'O compilador do .NET Framework 4.x (x64) não foi encontrado.' }
$work = Join-Path ([IO.Path]::GetTempPath()) ('the-matrix-build-' + [Guid]::NewGuid().ToString('N'))
$build = Join-Path $work 'bundle'
$payload = Join-Path $build 'payload'
try {
    New-Item -ItemType Directory -Path $build -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'installer\Install.ps1') -Destination $build
    Copy-Item -LiteralPath (Join-Path $root 'terminal') -Destination $payload -Recurse
    $assets = Join-Path $root 'installer\assets'

    & $csc /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll "/out:$payload\vibecode.exe" (Join-Path $root 'launcher\Vibecode.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar vibecode.exe.' }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipPath = Join-Path $work 'Matrix.Payload.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory($build, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)

    $manifest = Join-Path $root 'installer\setup.manifest'
    $setupSource = Join-Path $root 'installer\Setup.cs'
    $visualSource = Join-Path $root 'installer\Immersive.cs'
    $output = Join-Path $OutputDirectory 'The-Matrix-Setup-1.1.1.exe'
    & $csc /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.FileSystem.dll "/win32manifest:$manifest" "/win32icon:$assets\matrix.ico" "/resource:$assets\matrix-icon.png,Matrix.Icon.png" "/resource:$zipPath,Matrix.Payload.zip" "/out:$output" $setupSource $visualSource
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o instalador.' }
    $hash = Get-FileHash -LiteralPath $output -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($output))" |
        Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256.txt') -Encoding ascii
    Get-Item -LiteralPath $output
}
finally {
    if ((Test-Path -LiteralPath $work -PathType Container) -and (Split-Path -Leaf $work) -match '^the-matrix-build-[a-f0-9]{32}$' -and (Split-Path -Parent $work).TrimEnd('\') -eq ([IO.Path]::GetTempPath()).TrimEnd('\')) {
        Remove-Item -LiteralPath $work -Force -Recurse
    }
}
