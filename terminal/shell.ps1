param(
    [ValidateSet('Codex', 'Claude')][string]$Agent,
    [switch]$MatrixHelp
)

$matrixGit = Join-Path $PSScriptRoot 'Git'
$gitLocation = Join-Path $PSScriptRoot 'git-root.txt'
if (Test-Path -LiteralPath $gitLocation -PathType Leaf) {
    $matrixGit = (Get-Content -LiteralPath $gitLocation -Encoding UTF8 -Raw).Trim()
}

$matrixTools = @(
    (Join-Path $PSScriptRoot 'Agents\Codex\bin'),
    (Join-Path $PSScriptRoot 'Agents\Codex\codex-path'),
    (Join-Path $PSScriptRoot 'Agents\Claude'),
    (Join-Path $matrixGit 'cmd'),
    (Join-Path $env:APPDATA 'npm'),
    (Join-Path $env:USERPROFILE '.local\bin')
) | Where-Object { Test-Path -LiteralPath $_ -PathType Container }
$env:Path = (@($matrixTools) + @($env:Path -split ';') | Select-Object -Unique) -join ';'
$matrixBash = Join-Path $matrixGit 'usr\bin\bash.exe'
if (!$env:CLAUDE_CODE_GIT_BASH_PATH -and (Test-Path -LiteralPath $matrixBash)) {
    $env:CLAUDE_CODE_GIT_BASH_PATH = $matrixBash
}
if (!(Get-Command codex.exe -ErrorAction SilentlyContinue) -and (Get-Command codex.cmd -ErrorAction SilentlyContinue)) {
    Set-Alias -Name codex -Value codex.cmd
}

$script:matrixLauncher = Join-Path $PSScriptRoot 'vibecode.exe'

function vhelp {
    Write-Host ''
    Write-Host ' THE MATRIX  /  AJUDA' -ForegroundColor Green
    @(
        ' Ctrl+Shift+O     Escolher outra pasta e abrir uma janela dupla'
        ' Ctrl+Alt+H       Abrir esta ajuda em uma aba PowerShell'
        ' Ctrl+Shift+T     Abrir uma aba PowerShell'
        ' Ctrl+Alt+Setas   Alternar entre os dois painéis'
        ' F11              Alternar tela cheia'
        ''
        ' vgo <pasta>      Ir para uma pasta nesta aba'
        ' vls [pasta]      Listar pastas e arquivos'
        ' vup [níveis]     Subir um ou mais níveis'
        ' vback            Voltar à pasta anterior'
        ' vroot            Ir para a raiz do repositório Git'
        ' vopen [pasta]    Abrir Codex e Claude na pasta'
        ''
        ' No Explorador, pressione Ctrl+L e digite vibecode.'
        ' Em caminhos com espaços, use aspas. Exemplo: vgo "C:\Meus Projetos"'
        ' Pista: a pílula vermelha tem nome em inglês.'
        ''
    ) | ForEach-Object { Write-Host $_ }
}

function vgo {
    param([Parameter(Mandatory,Position=0)][string]$Path)
    $before = (Get-Location).ProviderPath
    Set-Location -LiteralPath $Path -ErrorAction Stop
    if ((Get-Location).ProviderPath -ne $before) { $script:matrixPreviousFolder = $before }
}

function vls {
    param([Parameter(Position=0)][string]$Path = '.')
    Get-ChildItem -LiteralPath $Path | Sort-Object -Property @{Expression='PSIsContainer';Descending=$true},Name |
        Format-Table Mode, Name -AutoSize
}

function vup {
    param([Parameter(Position=0)][ValidateRange(1,32)][int]$Levels = 1)
    $target = Get-Item -LiteralPath (Get-Location).ProviderPath -Force
    for ($i = 0; $i -lt $Levels -and $target.Parent; $i++) { $target = $target.Parent }
    vgo $target.FullName
}

function vback {
    if ($script:matrixPreviousFolder) { vgo $script:matrixPreviousFolder }
    else { Write-Host 'Ainda não há uma pasta anterior registrada por vgo, vup ou vroot.' }
}

function vroot {
    if (!(Get-Command git -ErrorAction SilentlyContinue)) { Write-Host 'Git não encontrado.'; return }
    $gitRoot = & git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0) { vgo $gitRoot }
    else { Write-Host 'A pasta atual não faz parte de um repositório Git.' }
}

function vopen {
    param([Parameter(Position=0)][string]$Path = '.')
    $folder = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (!$folder.PSIsContainer) { throw 'Escolha uma pasta.' }
    Start-Process -FilePath $script:matrixLauncher -WorkingDirectory $folder.FullName
}

function redpill {
    foreach ($line in @('[ OK ] Gato de Schrödinger respondeu antes do ping.', '  01101110 01100101 01101111', '  Siga o coelho: white-rabbit')) {
        Write-Host $line -ForegroundColor Green
        Start-Sleep -Milliseconds 180
    }
}

function white-rabbit {
    Write-Host @"
  (\_/)
  (o.o)   O kernel sonhou com 404 coelhos.
  (> <)   PSSST: rode deja-vu.
"@ -ForegroundColor Green
}

function deja-vu {
    Write-Host 'Um gato preto passou.' -ForegroundColor DarkGreen
    Start-Sleep -Milliseconds 350
    Write-Host 'Um gato preto passou.' -ForegroundColor Green
}

$posh = Join-Path $PSScriptRoot 'oh-my-posh.exe'
$theme = Join-Path $PSScriptRoot 'matrix.omp.json'
if (Test-Path -LiteralPath $posh) {
    & $posh init pwsh --eval --config $theme | Invoke-Expression
}

if ($MatrixHelp) { vhelp }
switch ($Agent) {
    'Codex'  { $Host.UI.RawUI.WindowTitle = 'Codex'; codex }
    'Claude' { $Host.UI.RawUI.WindowTitle = 'Claude'; claude }
}
