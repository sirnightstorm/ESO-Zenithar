param(
  [string] $ProjectPath = "ZenitharClient.csproj",
  [string] $Configuration = "Release",
  [string] $Runtime = "win-x64",
  [string] $Version = "0.3.0",
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
AppVerName=Zenithar v$Version
DefaultDirName={localappdata}\Zenithar
DefaultGroupName=Zenithar
Uninstallable=yes
PrivilegesRequired=lowest
Compression=lzma2/max
SolidCompression=yes
OutputBaseFilename=ZenitharSetup-$Version
OutputDir=$installerOut
AppMutex=ZenitharMutex
DisableProgramGroupPage=yes


[Files]
Source: "$publishDir\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Zenithar-Addon\build\Zenithar\*"; DestDir: "{userdocs}\Elder Scrolls Online\live\AddOns\Zenithar"; \
    Flags: recursesubdirs createallsubdirs; Tasks: installaddon

[Tasks]
Name: "installaddon"; Description: "Install ESO Zenithar AddOn"
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "Icons:"
Name: "desktopicon";   Description: "Create a &desktop icon";       GroupDescription: "Icons:"; Flags: unchecked

[Icons]
Name: "{autoprograms}\Zenithar Client"; Filename: "{app}\$appExe"; Tasks: startmenuicon
Name: "{userdesktop}\Zenithar Client"; Filename: "{app}\$appExe"; Tasks: desktopicon

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
if ([string]::IsNullOrWhiteSpace($ISCCPath)) {
  $possible = @(
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    # Per-user installs are usually located under LOCALAPPDATA
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Inno Setup 6\ISCC.exe"
  )
  foreach ($p in $possible) {
    if (-not [string]::IsNullOrWhiteSpace($p) -and (Test-Path $p)) {
      $ISCCPath = $p
      break
    }
  }
  if ([string]::IsNullOrWhiteSpace($ISCCPath)) {
    $pathCmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($pathCmd) { $ISCCPath = $pathCmd.Source }
  }
}

if ([string]::IsNullOrWhiteSpace($ISCCPath) -or -not (Test-Path $ISCCPath)) {

  $url = "https://jrsoftware.org/isdl.php"

  Write-Host ""
  Write-Host "Inno Setup compiler (ISCC.exe) was not found." -ForegroundColor Red
  Write-Host ""
  Write-Host "Please install Inno Setup 6 from the following link:"
  Write-Host $url -ForegroundColor Cyan
  Write-Host ""
  Write-Host "After installing, re-run this script."
  Write-Host "You can also pass the path manually using:"
  Write-Host "  -ISCCPath `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe`"" -ForegroundColor Yellow
  Write-Host ""

  exit 2
}

# Run Inno Setup Compiler
Write-Host "Compiling installer with ISCC: $ISCCPath"
& $ISCCPath $issPath
if ($LASTEXITCODE -ne 0) { Write-Error "ISCC failed"; exit $LASTEXITCODE }

$installerExe = Join-Path $installerOut "ZenitharSetup-$Version.exe"
Write-Host "Installer created: $installerExe"

$signTool = Get-Command signtool.exe -ErrorAction SilentlyContinue
if ($signTool) {
  & $signTool.Source sign /n "SirNightstorm" /fd SHA256 $installerExe
  if ($LASTEXITCODE -ne 0) {
    Write-Warning "signtool failed, but installer was created successfully."
  }
}
else {
  Write-Warning "signtool.exe not found. Skipping code signing."
}

Pop-Location