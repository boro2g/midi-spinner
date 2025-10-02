# PowerShell script for building release packages
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipPackaging
)

$ErrorActionPreference = "Stop"

Write-Host "Building Circular MIDI Generator v$Version" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Set up paths
$RootPath = Split-Path -Parent $PSScriptRoot
$SrcPath = Join-Path $RootPath "src"
$OutputPath = Join-Path $RootPath "dist"
$ProjectPath = Join-Path $SrcPath "CircularMidiGenerator"

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
Set-Location $RootPath
dotnet restore

# Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed. Build aborted."
        exit 1
    }
}

# Build for each platform
$Platforms = @(
    @{ Runtime = "win-x64"; Name = "Windows" },
    @{ Runtime = "osx-x64"; Name = "macOS Intel" },
    @{ Runtime = "osx-arm64"; Name = "macOS Apple Silicon" },
    @{ Runtime = "linux-x64"; Name = "Linux" }
)

foreach ($Platform in $Platforms) {
    $Runtime = $Platform.Runtime
    $PlatformName = $Platform.Name
    $PlatformOutput = Join-Path $OutputPath $Runtime
    
    Write-Host "Building for $PlatformName ($Runtime)..." -ForegroundColor Yellow
    
    dotnet publish $ProjectPath `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        --output $PlatformOutput `
        --no-restore `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:Version=$Version `
        -p:AssemblyVersion=$Version `
        -p:FileVersion=$Version
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $PlatformName"
        exit 1
    }
    
    Write-Host "✓ Build completed for $PlatformName" -ForegroundColor Green
}

# Create packages (unless skipped)
if (-not $SkipPackaging) {
    Write-Host "Creating distribution packages..." -ForegroundColor Yellow
    
    # Windows ZIP
    $WinPath = Join-Path $OutputPath "win-x64"
    $WinZip = Join-Path $OutputPath "CircularMidiGenerator-$Version-Windows.zip"
    Compress-Archive -Path "$WinPath\*" -DestinationPath $WinZip -Force
    Write-Host "✓ Created Windows package: $WinZip" -ForegroundColor Green
    
    # macOS Intel ZIP
    $MacIntelPath = Join-Path $OutputPath "osx-x64"
    $MacIntelZip = Join-Path $OutputPath "CircularMidiGenerator-$Version-macOS-Intel.zip"
    Compress-Archive -Path "$MacIntelPath\*" -DestinationPath $MacIntelZip -Force
    Write-Host "✓ Created macOS Intel package: $MacIntelZip" -ForegroundColor Green
    
    # macOS Apple Silicon ZIP
    $MacArmPath = Join-Path $OutputPath "osx-arm64"
    $MacArmZip = Join-Path $OutputPath "CircularMidiGenerator-$Version-macOS-AppleSilicon.zip"
    Compress-Archive -Path "$MacArmPath\*" -DestinationPath $MacArmZip -Force
    Write-Host "✓ Created macOS Apple Silicon package: $MacArmZip" -ForegroundColor Green
    
    # Linux TAR.GZ
    $LinuxPath = Join-Path $OutputPath "linux-x64"
    $LinuxTar = Join-Path $OutputPath "CircularMidiGenerator-$Version-Linux.tar.gz"
    Set-Location $LinuxPath
    tar -czf $LinuxTar *
    Write-Host "✓ Created Linux package: $LinuxTar" -ForegroundColor Green
}

# Generate checksums
Write-Host "Generating checksums..." -ForegroundColor Yellow
$ChecksumFile = Join-Path $OutputPath "checksums.txt"
Get-ChildItem $OutputPath -Filter "*.zip" | ForEach-Object {
    $Hash = Get-FileHash $_.FullName -Algorithm SHA256
    "$($Hash.Hash)  $($_.Name)" | Add-Content $ChecksumFile
}
Get-ChildItem $OutputPath -Filter "*.tar.gz" | ForEach-Object {
    $Hash = Get-FileHash $_.FullName -Algorithm SHA256
    "$($Hash.Hash)  $($_.Name)" | Add-Content $ChecksumFile
}

# Build summary
Write-Host "`nBuild Summary:" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Output Directory: $OutputPath" -ForegroundColor White

Get-ChildItem $OutputPath -Filter "*.zip" | ForEach-Object {
    $Size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name) ($Size MB)" -ForegroundColor Cyan
}
Get-ChildItem $OutputPath -Filter "*.tar.gz" | ForEach-Object {
    $Size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name) ($Size MB)" -ForegroundColor Cyan
}

Write-Host "`n✓ Build completed successfully!" -ForegroundColor Green
Write-Host "Packages are ready in: $OutputPath" -ForegroundColor Yellow