param(
  [string] $ProjectPath = "ZenitharClient.csproj",
  [string] $Configuration = "Release",
  [string] $Runtime = "win-x64",
  [string] $Version = "0.2.0",
  [bool] $SelfContained = $true,
  [bool] $PublishSingleFile = $true,
  [bool] $PublishTrimmed = $false,
  [string] $ISCCPath = ""
)

Set-StrictMode -Version Latest
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Push-Location $scriptDir

# Resolve project path robustly: accept absolute path, or look relative to script folder,
# then try the repository root (parent of script), then walk up parents if needed.
function Resolve-ProjectPath([string] $proj) {
    if ([System.IO.Path]::IsPathRooted($proj)) {
        return Resolve-Path $proj -ErrorAction Stop
    }

    # 1) relative to script dir
    $candidate = Join-Path $scriptDir $proj
    if (Test-Path $candidate) { return Resolve-Path $candidate }

    # 2) relative to repo root (one level up from script dir)
    $repoRoot = Resolve-Path ($scriptDir)
    $candidate = Join-Path $repoRoot $proj
    if (Test-Path $candidate) { return Resolve-Path $candidate }

    # 3) walk up parents up to 5 levels looking for project
    $current = $scriptDir
    for ($i = 0; $i -lt 5; $i++) {
        $candidate = Join-Path (Resolve-Path $current) $proj
        if (Test-Path $candidate) { return Resolve-Path $candidate }
        $current = Join-Path $current ".."
    }

    # fallback: let Resolve-Path raise the error so user sees what went wrong
    return Resolve-Path $proj -ErrorAction Stop
}

$projectFull = Resolve-ProjectPath $ProjectPath

# Publish output
$publishDir = Join-Path $scriptDir "build\publish\$Runtime\$Configuration"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

$dotnetArgs = @(
  "publish", $projectFull,
  "-c", $Configuration,
  "-r", $Runtime,
  "-o", $publishDir
)

if ($PublishSingleFile) { $dotnetArgs += "/p:PublishSingleFile=true" }
if ($SelfContained)     { $dotnetArgs += "/p:SelfContained=true" }
if ($PublishTrimmed)    { $dotnetArgs += "/p:PublishTrimmed=true" }

Write-Host "Publishing $projectFull -> $publishDir"
$pub = & dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit $LASTEXITCODE }

# Build Add-one
Write-Host "Building ESO AddOn project"
Push-Location "Zenithar-Addon"
& py src/python/build.py --version $Version
if ($LASTEXITCODE -ne 0) { Write-Error "add-on build failed"; exit $LASTEXITCODE }
Pop-Location

# Ensure there's an exe name (try to infer from project file)
$appExe = (Get-ChildItem -Path $publishDir -Filter *.exe | Select-Object -First 1).Name
if (-not $appExe) { Write-Error "No exe found in publish folder: $publishDir"; exit 1 }

# Create installer output dir
$installerOut = Join-Path $scriptDir "build\installer"
if (-not (Test-Path $installerOut)) { New-Item -ItemType Directory -Path $installerOut | Out-Null }

# Create Inno Setup script
# Help: https://jrsoftware.org/ishelp/
$issPath = Join-Path $scriptDir "build\\ZenitharClient.iss"
$issContent = @"
[Setup]
AppName=Zenithar
AppVersion=$Version
DefaultDirName={localappdata}\Zenithar
DefaultGroupName=Zenithar
Uninstallable=yes
PrivilegesRequired=lowest
Compression=lzma2/max
SolidCompression=yes
OutputBaseFilename=ZenitharSetup-$Version
OutputDir=$installerOut
AppMutex=ZenitharMutex

[Files]
Source: "$publishDir\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Zenithar-Addon\build\Zenithar\*"; DestDir: "{userdocs}\Elder Scrolls Online\live\AddOns\Zenithar"; \
    Flags: recursesubdirs createallsubdirs; Tasks: installaddon

[Tasks]
Name: "installaddon"; Description: "Install ESO Zenithar AddOn"
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Icons]
Name: "{group}\Zenithar"; Filename: "{app}\$appExe"
Name: "{userdesktop}\Zenithar"; Filename: "{app}\$appExe"; Tasks: desktopicon

[Run]
Filename: "{app}\$appExe"; Description: "Launch Zenithar"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup: Boolean;
begin
  if FindWindowByClassName('EsoClientWndClass') <> 0 then
  begin
    MsgBox('Please close Elder Scrolls Online before installing Zenithar.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
"@

Set-Content -Path $issPath -Value $issContent -Encoding UTF8
Write-Host "Wrote Inno script: $issPath"

# Locate ISCC.exe (Inno Setup Compiler)
if (-not $ISCCPath) {
  $possible = @(
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    # Per-user installs are usually located under LOCALAPPDATA
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Inno Setup 6\ISCC.exe"
  )
  foreach ($p in $possible) { if (Test-Path $p) { $ISCCPath = $p; break } }
  if (-not $ISCCPath) {
    $pathCmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($pathCmd) { $ISCCPath = $pathCmd.Source }
  }
}

if (-not (Test-Path $ISCCPath)) {
  Write-Error "ISCC.exe not found. Install Inno Setup or pass -ISCCPath 'C:\\Path\\To\\ISCC.exe'"
  exit 2
}

# Run Inno Setup Compiler
Write-Host "Compiling installer with ISCC: $ISCCPath"
& "$ISCCPath" $issPath 
if ($LASTEXITCODE -ne 0) { Write-Error "ISCC failed"; exit $LASTEXITCODE }

Write-Host "Installer created: $installerOut\\ZenitharSetup-$Version.exe"

& signtool.exe sign /n "SirNightstorm" /fd SHA256 "$installerOut\ZenitharSetup-$Version.exe"

Pop-Location