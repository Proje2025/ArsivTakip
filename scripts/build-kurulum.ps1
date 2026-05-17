$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$PublishDir = Join-Path $Root "bin\Release\net8.0-windows\publish"
$KurulumDir = Join-Path $Root "Kurulum"
$Iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"

Write-Host "1/3 Uygulama yayinlaniyor (temiz derleme)..." -ForegroundColor Cyan
dotnet clean "BirtanaArsivTakip.csproj" -c Release | Out-Null
dotnet publish "BirtanaArsivTakip.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $PublishDir

if (-not (Test-Path $Iscc)) {
    throw "Inno Setup bulunamadi: $Iscc"
}

Write-Host "2/3 Inno kurulum paketi olusturuluyor..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $KurulumDir | Out-Null
& $Iscc "Kurulum.iss"

Write-Host "3/3 setup.exe (bootstrapper) olusturuluyor..." -ForegroundColor Cyan
dotnet publish "Setup\ArsivTakip.Setup.csproj" -c Release -o $KurulumDir

$SetupExe = Join-Path $KurulumDir "setup.exe"
$KurulumExe = Join-Path $KurulumDir "ArsivTakipKurulum.exe"

if (-not (Test-Path $SetupExe)) {
    throw "setup.exe olusturulamadi"
}

if (-not (Test-Path $KurulumExe)) {
    throw "ArsivTakipKurulum.exe olusturulamadi"
}

Write-Host ""
Write-Host "Hazir:" -ForegroundColor Green
Write-Host "  $SetupExe"
Write-Host "  $KurulumExe"
Write-Host ""
Write-Host "GitHub yukleme:" -ForegroundColor Yellow
Write-Host "  gh release upload v1.1.1 `"$SetupExe`" `"$KurulumExe`" --clobber"
