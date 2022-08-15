# Inputs
param ([Parameter(Mandatory)] $CONFIGURATION, [Parameter(Mandatory)] $OUTPUT, $SYMBOLOUTPUT)

Write-Output "Output: $OUTPUT"

# Directories
$THISDIR = $pwd.path
$ROOT = (Get-Item $THISDIR).parent.parent.parent.FullName
$SRC = "$ROOT/src"
$GCM_SRC = "$SRC/shared/Git-Credential-Manager"
$BITBUCKET_UI_SRC = "$SRC/shared/Atlassian.Bitbucket.UI.Avalonia"
$GITHUB_UI_SRC = "$SRC/shared/GitHub.UI.Avalonia"
$GITLAB_UI_SRC = "$SRC/shared/GitLab.UI.Avalonia"

# Perform pre-execution checks
$PAYLOAD = "$OUTPUT"
if ($SYMBOLOUTPUT)
{
    $SYMBOLS = "$SYMBOLOUTPUT"
} else {
    $SYMBOLS = "$PAYLOAD.sym"
}

# Clean up any old payload and symbols directories
if (Test-Path -Path $PAYLOAD)
{
    Write-Output "Cleaning old payload directory '$PAYLOAD'..."
    Remove-Item -Recurse "$PAYLOAD" -Force
}

if (Test-Path -Path $SYMBOLS)
{
    Write-Output "Cleaning old symbols directory '$SYMBOLS'..."
    Remove-Item -Recurse "$SYMBOLS" -Force
}

# Ensure payload and symbol directories exist
mkdir -p "$PAYLOAD","$SYMBOLS"

# Publish core application executables
Write-Output "Publishing core application..."
dotnet publish "$GCM_SRC" `
	--configuration "$CONFIGURATION" `
	--runtime win-x86 `
	--self-contained `
	-p:PublishTrimmed=true `
	-p:TrimMode=copyused `
	--output "$PAYLOAD"

Write-Output "Publishing Bitbucket UI helper..."
dotnet publish "$BITBUCKET_UI_SRC" `
	--configuration "$CONFIGURATION" `
	--runtime win-x86 `
	--self-contained `
	-p:PublishTrimmed=true `
	-p:TrimMode=copyused `
	--output "$PAYLOAD"

Write-Output "Publishing GitHub UI helper..."
dotnet publish "$GITHUB_UI_SRC" `
	--configuration "$CONFIGURATION" `
	--runtime win-x86 `
	--self-contained `
	-p:PublishTrimmed=true `
	-p:TrimMode=copyused `
	--output "$PAYLOAD"

Write-Output "Publishing GitLab UI helper..."
dotnet publish "$GITLAB_UI_SRC" `
	--configuration "$CONFIGURATION" `
	--runtime win-x86 `
	--self-contained `
	-p:PublishTrimmed=true `
	-p:TrimMode=copyused `
	--output "$PAYLOAD"

# Collect symbols
Write-Output "Collecting managed symbols..."
Move-Item -Path "$PAYLOAD/*.pdb" -Destination "$SYMBOLS"

Write-Output "Layout complete."