[CmdletBinding()]
Param(
	[Parameter(Mandatory=$true)]
	[string] $Configuration,
	[Parameter(Mandatory=$true)]
	[string] $Version,
	[Parameter(Mandatory=$true)]
	[string] $Framework,
	[Parameter(Mandatory=$true)]
	[string] $Runtime,
	[Parameter(Mandatory=$true)]
	[string] $ISCCPath
)

# Stop on error
$ErrorActionPreference = "Stop"

###############
# Directories #
###############
$rootDir = Join-Path $PSScriptRoot "..\..\.."
$binBaseDir = Join-Path $rootDir "out\windows\Installer.Windows\bin\$Configuration\$Framework\$Runtime"
$pkgBaseDir = Join-Path $rootDir "out\windows\Installer.Windows\pkg\$Configuration\$Framework\$Runtime"
$payloadOutDir = Join-Path $binBaseDir "payload"
$symbolOutDir = Join-Path $binBaseDir "symbols"
$gcmProjDir = Join-Path $rootDir "src\shared\Git-Credential-Manager"
$signFilesProjDir = Join-Path $rootDir "src\windows\SignFiles.Windows"

#########
# Build #
#########
# Ensure old outputs have been deleted
Write-Host 'Deleting old binary outputs...'
Remove-Item -Force -Recurse -Path $binBaseDir -ErrorAction SilentlyContinue

Write-Host 'Deleting old installer outputs...'
Remove-Item -Force -Recurse -Path $pkgBaseDir -ErrorAction SilentlyContinue

# Create new intermediate and installer output directories
New-Item -Path $payloadOutDir -ItemType Directory
New-Item -Path $symbolOutDir -ItemType Directory
New-Item -Path $pkgBaseDir -ItemType Directory

# Publish application
Write-Host 'Publishing application...'
dotnet publish $gcmProjDir `
	-c $Configuration `
	-f $Framework `
	-r $Runtime `
	-o $payloadOutDir

# Collect symbols
Write-Host 'Collecting symbols...'
Move-Item -Path $payloadOutDir\*.pdb -Destination $symbolOutDir -Force

# Sign files
if ($env:SignType -ieq 'real') {
	Write-Host 'Signing binaries...'
	dotnet build $signFilesProjDir -p:RootDir=$payloadOutDir
}

# Create zip archives
Write-Host 'Creating archives...'
$binZipFile = Join-Path $pkgBaseDir "gcmcore-$Runtime-$Version.zip"
if (Test-Path -Path $binZipFile) {
	Write-Host 'Deleting old binary zip archive...'
	Remove-Item -Force -Recurse -Path $binZipFile
}
Compress-Archive -Path $payloadOutDir\* -DestinationPath $binZipFile

$symbolZipFile = Join-Path $pkgBaseDir "symbols-$Runtime-$version.zip"
if (Test-Path -Path $symbolZipFile) {
	Write-Host 'Deleting old symbol zip archive...'
	Remove-Item -Force -Recurse -Path $symbolZipFile
}
Compress-Archive -Path $symbolOutDir\* -DestinationPath $symbolZipFile

# Create installers
Write-Host 'Creating installers...'
& $ISCCPath /DPayloadDir=$payloadOutDir /DRuntime=$Runtime /DInstallTarget=system Setup.iss /O$pkgBaseDir
& $ISCCPath /DPayloadDir=$payloadOutDir /DRuntime=$Runtime /DInstallTarget=user Setup.iss /O$pkgBaseDir
