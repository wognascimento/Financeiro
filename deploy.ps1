param(
    [string]$ServerUploadPath = "root@192.168.0.49:/var/www/updates/downloads/financeiro/",
    [string]$UpdateBaseUrl = "http://192.168.0.49/downloads/financeiro",
    [string]$NetworkDeployPath = "\\192.168.0.4\sistemas\SIG",
    [string]$InnoCompiler = "",
    [string]$DotNetDesktopRuntimeInstallerPath = "",
    [System.Management.Automation.PSCredential]$NetworkCredential = $null,
    [string[]]$Changelog = @("Ajustes", "Melhorias"),
    [switch]$SkipServerUpload,
    [switch]$SkipNetworkCopy,
    [switch]$ForceDeploy
)

$ErrorActionPreference = "Stop"

$projectPath = $PSScriptRoot
$workspaceRoot = Resolve-Path (Join-Path $projectPath "..")
$projectFile = Join-Path $projectPath "Financeiro\Financeiro.csproj"
$publishPath = Join-Path $projectPath "publish"
$artifactsPath = Join-Path $projectPath "artifacts"
$installerPath = Join-Path $artifactsPath "installer"
$versionJsonPath = Join-Path $artifactsPath "version.json"
$redistPath = Join-Path $projectPath "redist"
$runtimeInstallerName = "windowsdesktop-runtime-9.0-win-x64.exe"
$runtimeInstallerTargetPath = Join-Path $redistPath $runtimeInstallerName

function Get-ApplicationVersion {
    $projContent = [xml](Get-Content $projectFile)
    $propertyGroups = @($projContent.Project.PropertyGroup)

    $version = ($propertyGroups | ForEach-Object { $_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)

    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = ($propertyGroups | ForEach-Object { $_.AssemblyVersion } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Versão não encontrada no .csproj. Informe <Version> ou <AssemblyVersion>."
    }

    return $version
}

function Resolve-InnoCompiler {
    param([string]$ConfiguredPath)

    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        $candidates += $ConfiguredPath
    }

    $candidates += Join-Path $workspaceRoot "tools\InnoSetup6\ISCC.exe"
    $candidates += "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "ISCC.exe não encontrado. Coloque o Inno Setup portable em '$workspaceRoot\tools\InnoSetup6' ou informe -InnoCompiler."
}

function Resolve-DotNetRuntimeInstaller {
    param([string]$ConfiguredPath)

    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        $candidates += $ConfiguredPath
    }

    $candidates += Join-Path $workspaceRoot "tools\dotnet\$runtimeInstallerName"
    $candidates += Join-Path $projectPath "redist\$runtimeInstallerName"

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    return ""
}

function Invoke-NativeCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Comando falhou com código ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Copy-ToNetwork {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [System.Management.Automation.PSCredential]$Credential = $null
    )

    if ([string]::IsNullOrWhiteSpace($DestinationPath)) {
        return
    }

    $destination = $DestinationPath.Trim().TrimEnd("\")
    $copyDestination = $destination
    $temporaryDriveName = $null

    try {
        if ($Credential -and $destination -match "^\\\\([^\\]+)\\([^\\]+)(\\.*)?$") {
            $networkShare = "\\$($Matches[1])\$($Matches[2])"
            $relativePath = $Matches[3]

            if ([string]::IsNullOrWhiteSpace($relativePath)) {
                $relativePath = ""
            }
            else {
                $relativePath = $relativePath.TrimStart("\")
            }

            $temporaryDriveName = "SIGDEPLOY$([Guid]::NewGuid().ToString("N").Substring(0, 8))"
            New-PSDrive -Name $temporaryDriveName -PSProvider FileSystem -Root $networkShare -Credential $Credential -ErrorAction Stop | Out-Null

            if ([string]::IsNullOrWhiteSpace($relativePath)) {
                $copyDestination = "${temporaryDriveName}:\"
            }
            else {
                $copyDestination = Join-Path "${temporaryDriveName}:\" $relativePath
            }
        }

        if (-not (Test-Path -LiteralPath $copyDestination -PathType Container)) {
            New-Item -ItemType Directory -Path $copyDestination -Force | Out-Null
        }

        Copy-Item -LiteralPath $SourcePath -Destination $copyDestination -Force
        Write-Host "Instalador copiado para: $destination"
    }
    catch {
        throw "Nao foi possivel copiar o instalador para '$destination'. Verifique se o compartilhamento existe, se o usuario tem permissao e, se necessario, execute com -NetworkCredential. Detalhe: $($_.Exception.Message)"
    }
    finally {
        if ($temporaryDriveName) {
            Remove-PSDrive -Name $temporaryDriveName -Force -ErrorAction SilentlyContinue
        }
    }
}

function Convert-ToVersion {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Versão vazia."
    }

    try {
        return [version]$Value
    }
    catch {
        throw "Versão inválida: $Value"
    }
}

function Test-ServerVersion {
    param(
        [string]$LocalVersion,
        [string]$BaseUrl,
        [switch]$Force
    )

    if ($Force) {
        Write-Host "Validação de versão do servidor ignorada por -ForceDeploy."
        return
    }

    $versionUrl = "$($BaseUrl.TrimEnd('/'))/version.json"
    Write-Host "Verificando versão publicada: $versionUrl"

    try {
        $response = Invoke-WebRequest -Uri $versionUrl -UseBasicParsing -TimeoutSec 20
    }
    catch {
        $statusCode = $null

        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode -eq 404) {
            Write-Host "Nenhum version.json encontrado no servidor. Deploy inicial permitido."
            return
        }

        throw "Não foi possível consultar a versão atual no servidor: $($_.Exception.Message)"
    }

    $serverInfo = $response.Content | ConvertFrom-Json
    $serverVersionText = $serverInfo.updateVersion

    if ([string]::IsNullOrWhiteSpace($serverVersionText)) {
        $serverVersionText = $serverInfo.currentVersion
    }

    if ([string]::IsNullOrWhiteSpace($serverVersionText)) {
        throw "O version.json do servidor não contém updateVersion nem currentVersion."
    }

    $local = Convert-ToVersion -Value $LocalVersion
    $server = Convert-ToVersion -Value $serverVersionText

    Write-Host "Versão local: $local"
    Write-Host "Versão no servidor: $server"

    if ($local -le $server) {
        throw "Deploy bloqueado. A versão local ($local) é igual ou inferior à versão publicada ($server). Atualize a versão do projeto ou use -ForceDeploy."
    }
}

$version = Get-ApplicationVersion
$resolvedInnoCompiler = Resolve-InnoCompiler -ConfiguredPath $InnoCompiler
$runtimeInstallerSourcePath = Resolve-DotNetRuntimeInstaller -ConfiguredPath $DotNetDesktopRuntimeInstallerPath
$zipFileName = "application-$version.zip"
$zipFullPath = Join-Path $artifactsPath $zipFileName

Write-Host "Projeto: $projectPath"
Write-Host "Versão: $version"
Write-Host "Inno Setup: $resolvedInnoCompiler"

if ([string]::IsNullOrWhiteSpace($runtimeInstallerSourcePath)) {
    Write-Host ".NET Desktop Runtime 9 não será embutido. Coloque '$runtimeInstallerName' em '$workspaceRoot\tools\dotnet' ou informe -DotNetDesktopRuntimeInstallerPath."
}
else {
    Write-Host ".NET Desktop Runtime 9: $runtimeInstallerSourcePath"
}

if (Test-Path $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

if (Test-Path $artifactsPath) {
    Remove-Item -LiteralPath $artifactsPath -Recurse -Force
}

if (Test-Path $redistPath) {
    Remove-Item -LiteralPath $redistPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
New-Item -ItemType Directory -Path $installerPath -Force | Out-Null
New-Item -ItemType Directory -Path $redistPath -Force | Out-Null

if (-not [string]::IsNullOrWhiteSpace($runtimeInstallerSourcePath)) {
    Copy-Item -Path $runtimeInstallerSourcePath -Destination $runtimeInstallerTargetPath -Force
}

Invoke-NativeCommand -FilePath "dotnet" -Arguments @("publish", $projectFile, "-c", "Release", "-o", $publishPath)

Push-Location $projectPath
try {
    Invoke-NativeCommand -FilePath $resolvedInnoCompiler -Arguments @("Setup.iss", "/DMyAppVersion=$version")
}
finally {
    Pop-Location
}

Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipFullPath -Force

$updateJson = @{
    currentVersion = $version
    updateVersion = $version
    updateUrl = "$($UpdateBaseUrl.TrimEnd('/'))/$zipFileName"
    changelog = $Changelog
    releaseDate = (Get-Date).ToString("yyyy-MM-dd")
    minimumCompatibleVersion = "1.0.0"
} | ConvertTo-Json

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($versionJsonPath, $updateJson, $utf8NoBom)

$installerFile = Get-ChildItem -Path $installerPath -Filter "FinanceiroSetup-$version.exe" | Select-Object -First 1

if (-not $installerFile) {
    throw "Instalador não foi gerado em $installerPath"
}

if (-not $SkipServerUpload) {
    Test-ServerVersion -LocalVersion $version -BaseUrl $UpdateBaseUrl -Force:$ForceDeploy

    $remoteBasePath = $ServerUploadPath.TrimEnd("/")
    $tempZipRemotePath = "$remoteBasePath/$zipFileName.tmp"
    $remoteHost = $remoteBasePath.Split(":")[0]
    $remoteDirectory = $remoteBasePath.Split(":")[1]

    Invoke-NativeCommand -FilePath "scp" -Arguments @($zipFullPath, $tempZipRemotePath)
    Invoke-NativeCommand -FilePath "ssh" -Arguments @($remoteHost, "mv '$remoteDirectory/$zipFileName.tmp' '$remoteDirectory/$zipFileName'")
    Invoke-NativeCommand -FilePath "scp" -Arguments @($versionJsonPath, "$remoteBasePath/version.json")
}

if (-not $SkipNetworkCopy) {
    Copy-ToNetwork -SourcePath $installerFile.FullName -DestinationPath $NetworkDeployPath -Credential $NetworkCredential
}

Write-Host "Deploy concluído para versão $version"
Write-Host "Pacote de atualização: $zipFullPath"
Write-Host "JSON de atualização: $versionJsonPath"
Write-Host "Instalador: $($installerFile.FullName)"
