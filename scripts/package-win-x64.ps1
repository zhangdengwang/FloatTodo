$ErrorActionPreference = "Stop"

function Get-ProjectRoot {
    $scriptDirectory = Split-Path -Parent $PSCommandPath

    if (Test-Path (Join-Path $scriptDirectory "src\FloatTodo.App\FloatTodo.App.csproj")) {
        return $scriptDirectory
    }

    $parentDirectory = Split-Path -Parent $scriptDirectory
    if (Test-Path (Join-Path $parentDirectory "src\FloatTodo.App\FloatTodo.App.csproj")) {
        return $parentDirectory
    }

    throw "Unable to locate project root. Run this script from project root or scripts directory."
}

function ConvertFrom-Base64Utf8 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($Value))
}

function Remove-PrivatePublishFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PublishDirectory
    )

    $privateDirectories = @("data")
    foreach ($directoryName in $privateDirectories) {
        Get-ChildItem -Path $PublishDirectory -Recurse -Force -Directory -Filter $directoryName -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force
    }

    $privateFiles = @(
        "*.log",
        ".env",
        ".env.*",
        "local-settings.json",
        "appsettings.Development.json",
        "secrets.json"
    )

    foreach ($filePattern in $privateFiles) {
        Get-ChildItem -Path $PublishDirectory -Recurse -Force -File -Filter $filePattern -ErrorAction SilentlyContinue |
            Remove-Item -Force
    }
}

$projectRoot = Get-ProjectRoot
$projectPath = Join-Path $projectRoot "src\FloatTodo.App\FloatTodo.App.csproj"
$distDirectory = Join-Path $projectRoot "dist"
$publishDirectory = Join-Path $distDirectory "FloatTodo-win-x64"
$zipPath = Join-Path $distDirectory "FloatTodo-win-x64.zip"
$exePath = Join-Path $publishDirectory "FloatTodo.App.exe"

$readmeFileName = ConvertFrom-Base64Utf8 "UkVBRE1FX+i/kOihjOivtOaYji50eHQ="
$readmeContent = ConvertFrom-Base64Utf8 "RmxvYXRUb2RvIOi/kOihjOivtOaYjgoKMS4g5Y+M5Ye7IEZsb2F0VG9kby5BcHAuZXhlIOWNs+WPr+i/kOihjOOAggoyLiDmnKzniYjmnKzkuLogV2luZG93cyB4NjQg6Ieq5YyF5ZCr54mI5pys77yM5LiN6ZyA6KaB6aKd5aSW5a6J6KOFIC5ORVTjgIIKMy4g56iL5bqP5ZCv5Yqo5ZCO5Lya5pi+56S65qGM5a6g5oKs5rWu56qX44CCCjQuIOW3pumUruaMieS9j+ahjOWuoOWPr+S7peaLluWKqOOAggo1LiDlj7PplK7moYzlrqDlj6/ku6XmiZPlvIDlip/og73oj5zljZXjgIIKNi4gQUkg5ouG6Kej5Yqf6IO96ZyA6KaB6YWN572uIERlZXBTZWVrIEFQSSBLZXkg5ZKM572R57uc44CCCjcuIOaZrumAmuS7u+WKoeOAgemhueebruOAgeaXpeW4uOiusOW9leOAgeaPkOmGkuWKn+iDveS4jemcgOimgSBBUEkgS2V544CCCjguIOWmguaenOadgOavkui9r+S7tummluasoei/kOihjOaPkOekuuacquefpeeoi+W6j++8jOivt+mAieaLqeWFgeiuuOi/kOihjOOAggo="
$readmePath = Join-Path $publishDirectory $readmeFileName

Set-Location $projectRoot

Write-Host "Project root: $projectRoot"

$runningProcesses = Get-Process -Name "FloatTodo.App" -ErrorAction SilentlyContinue
if ($runningProcesses) {
    Write-Host "Stopping running FloatTodo.App processes..."
    $runningProcesses | Stop-Process -Force
}

Write-Host "Running dotnet build..."
dotnet build

if (Test-Path $publishDirectory) {
    $resolvedPublishDirectory = Resolve-Path $publishDirectory
    $resolvedDistDirectory = Resolve-Path $distDirectory -ErrorAction SilentlyContinue

    if ($resolvedDistDirectory -and -not $resolvedPublishDirectory.Path.StartsWith($resolvedDistDirectory.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Publish directory is outside dist. Cleanup stopped: $publishDirectory"
    }

    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

Write-Host "Running dotnet publish for Windows x64 self-contained single-file package..."
dotnet publish $projectPath `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o $publishDirectory

Remove-PrivatePublishFiles -PublishDirectory $publishDirectory

[System.IO.File]::WriteAllText($readmePath, $readmeContent, [System.Text.Encoding]::UTF8)

if (-not (Test-Path $exePath)) {
    throw "Publish failed: executable was not found at $exePath"
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Write-Host "Creating zip..."
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -Force

if (-not (Test-Path $zipPath)) {
    throw "Packaging failed: zip was not generated at $zipPath"
}

Write-Host ""
Write-Host "Packaging completed."
Write-Host "Publish directory: $publishDirectory"
Write-Host "Zip path: $zipPath"
Write-Host "Executable: $exePath"
