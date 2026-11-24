param(
    [Parameter(Mandatory)][string]$Project,
    [switch]$AutoStart,
    [switch]$HideProject,
    [string]$TestRoot,
    [string]$TestCache
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$Utf8 = [Text.UTF8Encoding]::new($false)
$test = ![string]::IsNullOrWhiteSpace($TestRoot)
$programs = if ($test) { [IO.Path]::GetFullPath($TestRoot) } else { Join-Path $env:LOCALAPPDATA 'Programs' }
$app = Join-Path $programs 'MatrixTerminal'
$stage = Join-Path $programs ('MatrixTerminal.setup-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $PSScriptRoot 'payload'
$tarExe = Join-Path $env:WINDIR 'System32\tar.exe'
$curlExe = Join-Path $env:WINDIR 'System32\curl.exe'
$appKey = 'Software\Microsoft\Windows\CurrentVersion\App Paths\vibecode.exe'
$launcher = Join-Path $app 'vibecode.exe'

function Write-Step([int]$Percent, [string]$Message) {
    Write-Output "STEP ${Percent} $Message"
}

function Receive-Package([string]$Name, [string]$Url, [string]$Sha256) {
    $target = Join-Path $stage $Name
    if ($test -and $TestCache) {
        Copy-Item -LiteralPath (Join-Path $TestCache $Name) -Destination $target
    } else {
        & $curlExe --silent --show-error --fail --location --proto '=https' --proto-redir '=https' --retry 2 --connect-timeout 25 --max-time 600 --output $target $Url
        if ($LASTEXITCODE -ne 0) { throw "Falha ao baixar $Name. Verifique a rede e tente novamente." }
    }
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $Sha256) {
        throw "A soma SHA-256 de $Name divergiu. O pacote não foi executado."
    }
    return $target
}

function Read-ClauseText([string]$Relative, [string]$TestName) {
    if ($test -and $TestCache) { return Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $TestCache $TestName) }
    $endpoint = "https://downloads.claude.ai/claude-code-releases/$Relative"
    $result = & $curlExe --silent --show-error --fail --location --proto '=https' --proto-redir '=https' --retry 2 --max-time 40 $endpoint
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível consultar o servidor de downloads da Anthropic.' }
    return ($result -join "`n")
}

function Expand-Package([string]$Archive,[string]$Destination,[int]$Strip = 0) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    & $tarExe -xf $Archive -C $Destination "--strip-components=$Strip"
    if ($LASTEXITCODE -ne 0) { throw "Não foi possível extrair $(Split-Path -Leaf $Archive)." }
    Remove-Item -LiteralPath $Archive
}

function Test-Programs([string]$Root) {
    $executables = @(
        'WezTerm\wezterm.exe',
        'PowerShell\pwsh.exe',
        'oh-my-posh.exe',
        'Git\cmd\git.exe',
        'Git\usr\bin\bash.exe',
        'Agents\Codex\bin\codex.exe',
        'Agents\Claude\claude.exe'
    )
    foreach ($relative in $executables) {
        $exe = Join-Path $Root $relative
        Get-Item -LiteralPath $exe | Out-Null
        $flags = if ($relative -eq 'oh-my-posh.exe') { 'version' } else { '--version' }
        $version = & $exe $flags
        if ($LASTEXITCODE -ne 0) { throw "Falha ao testar $relative." }
        Write-Output ('OK ' + ($version | Select-Object -First 1))
    }
    foreach ($relative in @('Agents\Codex\codex-resources\codex-command-runner.exe','Agents\Codex\codex-resources\codex-windows-sandbox-setup.exe')) {
        Get-Item -LiteralPath (Join-Path $Root $relative) | Out-Null
    }
    foreach ($config in @('wezterm.lua','agents.lua')) {
        $keys = & (Join-Path $Root 'WezTerm\wezterm.exe') --config-file (Join-Path $Root $config) show-keys
        if ($LASTEXITCODE -ne 0 -or !($keys -match 'F11.+ToggleFullScreen') -or @($keys -match 'EmitEvent\("user-defined-').Count -lt 2) {
            throw "A configuração $config não passou no teste."
        }
    }
}

function Set-MatrixLink([string]$Path,[string]$Target,[string]$Folder,[string]$Arguments,[string]$Description) {
    if (Test-Path -LiteralPath $Path) {
        if ($menuShell.CreateShortcut($Path).TargetPath -ne $Target) { throw "O atalho $Path pertence a outro aplicativo." }
    }
    $link = $menuShell.CreateShortcut($Path)
    $link.TargetPath = $Target
    $link.Arguments = $Arguments
    $link.WorkingDirectory = $Folder
    $link.IconLocation = (Join-Path $app 'WezTerm\wezterm-gui.exe') + ',0'
    $link.Description = $Description
    $link.Save()
    $saved = $menuShell.CreateShortcut($Path)
    if ($saved.TargetPath -ne $Target -or $saved.WorkingDirectory -ne $Folder) {
        throw "Falha ao verificar o atalho $Path."
    }
}

try {
    if ([Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne 'X64' -or [Environment]::OSVersion.Version.Build -lt 17763) {
        throw 'Este instalador requer Windows 10 (1809 ou posterior) ou Windows 11 em um PC x64.'
    }
    if (!(Test-Path -LiteralPath $tarExe) -or !(Test-Path -LiteralPath $curlExe)) {
        throw 'Esta instalação requer o curl e o tar que acompanham o Windows.'
    }
    $Project = [IO.Path]::GetFullPath($Project)
    if ($HideProject -and $Project.TrimEnd('\') -in @([IO.Path]::GetPathRoot($Project).TrimEnd('\'),$env:USERPROFILE,$env:WINDIR,$env:LOCALAPPDATA)) {
        throw 'Escolha uma pasta de projetos específica para ocultar.'
    }
    if (Test-Path -LiteralPath $Project -PathType Leaf) { throw 'O caminho escolhido é um arquivo. Escolha uma pasta.' }
    if (!$test) {
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($appKey)
        if ($key) {
            try {
                if ($key.GetValue('') -ne $launcher) { throw 'O comando vibecode já está registrado para outra instalação. Use este assistente em outro PC ou conta.' }
            } finally { $key.Dispose() }
        }
    }
    if (Test-Path -LiteralPath $app) {
        if (!(Test-Path -LiteralPath (Join-Path $app 'matrix-setup.version'))) {
            throw 'Já existe uma instalação do The Matrix neste PC. Use este instalador no novo PC.'
        }
        Write-Step 80 'Instalação encontrada. Verificando os componentes.'
        Test-Programs $app
    } else {
        New-Item -ItemType Directory -Path $stage -Force | Out-Null
        $bundle = Join-Path $stage 'bundle'
        Copy-Item -LiteralPath $payload -Destination $bundle -Recurse

        Write-Step 8 'Baixando e verificando WezTerm'
        $file = Receive-Package 'wezterm.zip' 'https://github.com/wezterm/wezterm/releases/download/20240203-110809-5046fc22/WezTerm-windows-20240203-110809-5046fc22.zip' '57e5d03b585303d81e8b8e96d1230362852eb39aca92b3b29c7a42cfb82f9ac4'
        Expand-Package $file (Join-Path $bundle 'WezTerm') 1

        Write-Step 22 'Baixando e verificando PowerShell 7'
        $file = Receive-Package 'powershell.zip' 'https://github.com/PowerShell/PowerShell/releases/download/v7.7.0/PowerShell-7.7.0-win-x64.zip' '7c74e03b20a75b59715847439d046a6214b1863ee79b187872980f139bb4a05f'
        Expand-Package $file (Join-Path $bundle 'PowerShell')

        Write-Step 36 'Baixando e verificando Oh My Posh'
        $file = Receive-Package 'oh-my-posh.exe' 'https://github.com/JanDeDobbeleer/oh-my-posh/releases/download/v33.4.0/posh-windows-amd64.exe' 'c81371eb0b4863aef3d3e75fb59dc13a2e561267e226ce0f4cd382b28dbe7822'
        Move-Item -LiteralPath $file -Destination (Join-Path $bundle 'oh-my-posh.exe')

        Write-Step 45 'Baixando e verificando Git e Git Bash'
        $file = Receive-Package 'PortableGit.7z.exe' 'https://github.com/git-for-windows/git/releases/download/v2.56.1.windows.1/PortableGit-2.56.1-64-bit.7z.exe' '9dd04a608ceb5f6ab32a73709e0ecb159d211da71b697c45ff81f6dd0c6b49c7'
        $portableGit = Join-Path $stage 'PortableGit'
        $extractor = Start-Process -FilePath $file -ArgumentList '-y', '-gm2' -WorkingDirectory $stage -WindowStyle Hidden -Wait -PassThru
        if ($extractor.ExitCode -ne 0 -or !(Test-Path -LiteralPath $portableGit -PathType Container)) {
            throw "Não foi possível extrair o Git portátil (código $($extractor.ExitCode))."
        }
        Move-Item -LiteralPath $portableGit -Destination (Join-Path $bundle 'Git')
        Remove-Item -LiteralPath $file

        Write-Step 58 'Baixando e verificando Codex'
        $file = Receive-Package 'codex.tar.gz' 'https://github.com/openai/codex/releases/download/rust-v0.172.0/codex-package-x86_64-pc-windows-msvc.tar.gz' 'b723674d7caf26a3f55121a02d6cad8f3ac8ddf5860747a6edde1c904cf593af'
        Expand-Package $file (Join-Path $bundle 'Agents\Codex')

        Write-Step 73 'Baixando e verificando Claude Code'
        $version = (Read-ClauseText 'stable' 'claude-stable.txt').Trim()
        if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'A versão informada pela Anthropic é inválida.' }
        $manifest = Read-ClauseText "$version/manifest.json" 'claude.json' | ConvertFrom-Json
        $checksum = $manifest.platforms.'win32-x64'.checksum
        if ($checksum -notmatch '^[a-fA-F0-9]{64}$') { throw 'O manifesto oficial do Claude Code não contém um SHA-256 válido.' }
        $file = Receive-Package 'claude.exe' "https://downloads.claude.ai/claude-code-releases/$version/win32-x64/claude.exe" $checksum
        $claudeFolder = Join-Path $bundle 'Agents\Claude'
        New-Item -ItemType Directory -Path $claudeFolder | Out-Null
        Move-Item -LiteralPath $file -Destination (Join-Path $claudeFolder 'claude.exe')

        Write-Step 88 'Testando os terminais e o tema'
        Test-Programs $bundle
        [IO.File]::WriteAllText((Join-Path $bundle 'matrix-setup.version'),'1.0.1',$Utf8)
        [IO.Directory]::Move($bundle, $app)
    }

    Write-Step 95 'Criando atalhos e pasta de trabalho'
    [IO.Directory]::CreateDirectory($Project) | Out-Null
    if ($HideProject) {
        $folder = Get-Item -LiteralPath $Project -Force
        $folder.Attributes = $folder.Attributes -bor [IO.FileAttributes]::Hidden
    }
    $menu = if ($test) { Join-Path $programs 'Menu' } else { [Environment]::GetFolderPath('Programs') }
    $startup = if ($test) { Join-Path $programs 'Startup' } else {
        [Environment]::GetFolderPath([Environment+SpecialFolder]::Startup,[Environment+SpecialFolderOption]::DoNotVerify)
    }
    if (!$menu -or !$startup) { throw 'O Windows não encontrou o Menu Iniciar da conta atual.' }
    [IO.Directory]::CreateDirectory($menu) | Out-Null
    $menuShell = New-Object -ComObject WScript.Shell
    Set-MatrixLink (Join-Path $menu 'The Matrix.lnk') $launcher $Project '' 'Codex e Claude Code em tela cheia'
    $gui = Join-Path $app 'WezTerm\wezterm-gui.exe'
    $theme = Join-Path $app 'wezterm.lua'
    $args = '--config-file "{0}" start --always-new-process' -f $theme
    Set-MatrixLink (Join-Path $menu 'The Matrix - PowerShell.lnk') $gui $Project $args 'PowerShell 7 com tema CRT'
    $startupLink = Join-Path $startup 'The Matrix.lnk'
    if ($AutoStart) {
        [IO.Directory]::CreateDirectory($startup) | Out-Null
        Set-MatrixLink $startupLink $launcher $Project '' 'Abrir The Matrix ao entrar no Windows'
    } elseif ((Test-Path -LiteralPath $startupLink) -and $menuShell.CreateShortcut($startupLink).TargetPath -eq $launcher) {
        Remove-Item -LiteralPath $startupLink
    }
    if (!$test) {
        $reg = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($appKey)
        try { $reg.SetValue('', $launcher, [Microsoft.Win32.RegistryValueKind]::String) }
        finally { $reg.Dispose() }
    }
    Write-Step 100 'Instalação concluída'
} catch {
    Write-Output ('ERROR ' + $_.Exception.GetBaseException().Message)
    exit 1
} finally {
    if ((Test-Path -LiteralPath $stage -PathType Container) -and (Split-Path -Parent $stage) -eq $programs -and (Split-Path -Leaf $stage) -match '^MatrixTerminal\.setup-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}
