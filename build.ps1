param(
    [string]$Version = "",
    [string]$CertSubject = "CN=Windows Desktop Enhance, O=IceboundNaNa",
    [string]$CertThumbprint = "",
    [switch]$NoSign
)

$ErrorActionPreference = "Stop"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    throw "MSBuild.exe not found at $msbuild"
}

$versionFile = Join-Path $PSScriptRoot "VERSION"
if ([string]::IsNullOrWhiteSpace($Version)) {
    if (Test-Path $versionFile) {
        $Version = (Get-Content $versionFile -Raw).Trim()
    } else {
        $Version = "1.0.0"
    }
} else {
    Set-Content -Path $versionFile -Value $Version -NoNewline
}

$parts = $Version.Split('.')
while ($parts.Count -lt 4) { $parts += "0" }
$ver4 = ($parts[0..3] -join '.')
Write-Host "Project Version: $Version (Assembly/FileVersion: $ver4)" -ForegroundColor Cyan

foreach ($asmInfo in @(
    (Join-Path $PSScriptRoot "src\Properties\AssemblyInfo.cs"),
    (Join-Path $PSScriptRoot "installer\Properties\AssemblyInfo.cs")
)) {
    if (Test-Path $asmInfo) {
        $content = Get-Content $asmInfo -Raw
        $content = [regex]::Replace($content, 'AssemblyVersion\("[^"]+"\)', "AssemblyVersion(`"$ver4`")")
        $content = [regex]::Replace($content, 'AssemblyFileVersion\("[^"]+"\)', "AssemblyFileVersion(`"$ver4`")")
        $content = [regex]::Replace($content, 'AssemblyInformationalVersion\("[^"]+"\)', "AssemblyInformationalVersion(`"$Version`")")
        Set-Content -Path $asmInfo -Value $content -NoNewline
    }
}

function Get-OrCreateSigningCert {
    param([string]$Subject, [string]$Thumbprint)

    if (-not [string]::IsNullOrWhiteSpace($Thumbprint)) {
        $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Thumbprint -eq $Thumbprint } | Select-Object -First 1
        if (-not $cert) {
            $cert = Get-ChildItem Cert:\LocalMachine\My -CodeSigningCert | Where-Object { $_.Thumbprint -eq $Thumbprint } | Select-Object -First 1
        }
        if (-not $cert) { throw "Code signing certificate with thumbprint $Thumbprint not found." }
        return $cert
    }

    $existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $Subject -and $_.NotAfter -gt (Get-Date).AddDays(7) } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($existing) {
        return $existing
    }

    Write-Host "Creating local self-signed Code Signing certificate ($Subject)..." -ForegroundColor Yellow
    return New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $Subject `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(5)
}

function Sign-Executable {
    param([string]$FilePath, [System.Security.Cryptography.X509Certificates.X509Certificate2]$Cert)

    $sig = $null
    try {
        $sig = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $Cert -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com" -ErrorAction Stop
    } catch {
        Write-Host "  Timestamp server unavailable, signing without RFC3161 timestamp..." -ForegroundColor DarkYellow
        $sig = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $Cert -HashAlgorithm SHA256 -ErrorAction Stop
    }

    if ($sig.Status -eq "NotSigned" -or $sig.Status -eq "HashMismatch") {
        throw "Failed to sign $FilePath : $($sig.StatusMessage)"
    }
    Write-Host "  Signed: $(Split-Path $FilePath -Leaf) [Thumbprint=$($Cert.Thumbprint.Substring(0,8))..., Status=$($sig.Status)]" -ForegroundColor Cyan
}

$portableExe = Join-Path $PSScriptRoot "Windows Desktop Enhance.exe"
$setupExe = Join-Path $PSScriptRoot "Windows Desktop Enhance Setup.exe"

Write-Host "[1/2] Building Portable Edition (Windows Desktop Enhance.exe)..." -ForegroundColor Cyan
& $msbuild "$PSScriptRoot\src\WindowsDesktopEnhance.csproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$signingCert = $null
if (-not $NoSign) {
    $signingCert = Get-OrCreateSigningCert -Subject $CertSubject -Thumbprint $CertThumbprint
    Sign-Executable -FilePath $portableExe -Cert $signingCert
}

Write-Host "[2/2] Building Installer Edition (Windows Desktop Enhance Setup.exe)..." -ForegroundColor Cyan
& $msbuild "$PSScriptRoot\installer\WindowsDesktopEnhance.Setup.csproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $NoSign -and $signingCert) {
    Sign-Executable -FilePath $setupExe -Cert $signingCert
}

Write-Host ""
Write-Host "Build succeeded!" -ForegroundColor Green
Write-Host "  Portable  : $portableExe" -ForegroundColor Green
Write-Host "  Installer : $setupExe" -ForegroundColor Green
