<# 
dotnet-project-report.ps1 (PowerShell 5.1 compatible)
Generates ProjectReport.md summarizing a .NET repo. 
- No self-invocation
- No PowerShell 7-only syntax
- ASCII output only
- StrictMode-safe Count handling
#>

param(
  [string]$Root = ".",
  [string]$OutFile = "ProjectReport.md",
  [switch]$IncludeTransitivePackages
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$RegexOptions = [System.Text.RegularExpressions.RegexOptions]

function Read-CsprojMeta {
  param([string]$Path)

  $xmlText = Get-Content -Raw -Encoding UTF8 $Path
  [xml]$xml = $xmlText

  function FirstInnerText([string[]]$xpaths) {
    foreach ($xp in $xpaths) {
      $n = $xml.SelectSingleNode($xp)
      if ($n -and $n.InnerText) { return $n.InnerText.Trim() }
    }
    return $null
  }

  # Work with any namespace by matching local-name()
  $tf  = FirstInnerText @("//*[local-name()='TargetFramework']")
  $tfs = FirstInnerText @("//*[local-name()='TargetFrameworks']")

  $frameworks = @()
  if ($tf)  { $frameworks += $tf }
  if ($tfs) { $frameworks += ($tfs -split ';') }

  $assemblyName = FirstInnerText @("//*[local-name()='AssemblyName']")
  $outputType   = FirstInnerText @("//*[local-name()='OutputType']")

  if (-not $assemblyName -or $assemblyName -eq "") {
    $assemblyName = [IO.Path]::GetFileNameWithoutExtension($Path)
  }
  if (-not $outputType -or $outputType -eq "") {
    $outputType = "Library/Default"
  }

  [pscustomobject]@{
    TargetFrameworks = ($frameworks | Where-Object { $_ -and $_.Trim() -ne "" } | Sort-Object -Unique)
    AssemblyName     = $assemblyName
    OutputType       = $outputType
  }
}

function Count-CodeStats {
  param([string]$ProjectDir)

  $files = Get-ChildItem -Path $ProjectDir -Recurse -Include *.cs -File -ErrorAction SilentlyContinue
  $totalLoc = 0
  $classes = 0; $interfaces = 0; $enums = 0; $records = 0

  foreach ($f in @($files)) {
    $text = Get-Content -Raw -Encoding UTF8 $f.FullName
    $totalLoc += ($text -split "`n").Count
    $classes   += ([regex]::Matches($text,'^\s*(public|internal|protected|private)?\s*(partial\s+)?class\s+\w+',$RegexOptions::Multiline)).Count
    $interfaces+= ([regex]::Matches($text,'^\s*(public|internal|protected|private)?\s*interface\s+\w+',$RegexOptions::Multiline)).Count
    $enums     += ([regex]::Matches($text,'^\s*(public|internal|protected|private)?\s*enum\s+\w+',$RegexOptions::Multiline)).Count
    $records   += ([regex]::Matches($text,'^\s*(public|internal|protected|private)?\s*(partial\s+)?record\s+\w+',$RegexOptions::Multiline)).Count
  }

  [pscustomobject]@{ 
    Files      = (@($files).Count)
    LOC        = $totalLoc
    Classes    = $classes
    Interfaces = $interfaces
    Enums      = $enums
    Records    = $records
  }
}

function Detect-WebApiBits {
  param([string]$ProjectDir)

  $controllers = Get-ChildItem -Path $ProjectDir -Recurse -Include *Controller.cs -File -ErrorAction SilentlyContinue
  $allCs = Get-ChildItem -Path $ProjectDir -Recurse -Include *.cs -File -ErrorAction SilentlyContinue | ForEach-Object FullName

  $minimal = @()
  if ($allCs) { $minimal = Select-String -Path $allCs -Pattern '\bMap(Get|Post|Put|Delete|Patch)\s*\(' -ErrorAction SilentlyContinue }

  $startup = Get-ChildItem -Path $ProjectDir -Recurse -Include Startup.cs,Program.cs -File -ErrorAction SilentlyContinue

  [pscustomobject]@{
    ControllersCount = (@($controllers).Count)
    MinimalApiApprox = (@($minimal).Count)
    StartupOrProgram = (@($startup | ForEach-Object FullName))
  }
}

function Detect-EntityFramework {
  param([string]$ProjectDir)

  $allCs = Get-ChildItem -Path $ProjectDir -Recurse -Include *.cs -File -ErrorAction SilentlyContinue | ForEach-Object FullName

  $dbContexts = @()
  if ($allCs) { $dbContexts = Select-String -Path $allCs -Pattern '\bclass\s+\w+DbContext\b' -ErrorAction SilentlyContinue }

  $migrationsDir = Get-ChildItem -Path $ProjectDir -Recurse -Directory -Filter "Migrations" -ErrorAction SilentlyContinue

  [pscustomobject]@{
    DbContextCount = (@($dbContexts).Count)
    HasMigrations  = [bool]$migrationsDir
  }
}

function Get-Packages {
  param([string]$CsprojPath, [switch]$IncludeTransitive)

  $args = @("list", $CsprojPath, "package", "--verbosity","quiet")
  if ($IncludeTransitive) { $args += "--include-transitive" }

  try { $out = & dotnet @args 2>$null } catch { return @() }
  if (-not $out) { return @() }

  $rows = foreach ($line in $out) {
    if ($line -match '^\s*>\s*Project\s*:') { continue }
    if ($line -match '^\s*(?<name>[A-Za-z0-9\.\-_]+)\s+(?<version>[0-9][^\s]*)') {
      [pscustomobject]@{ Name=$Matches.name; Version=$Matches.version }
    }
  }

  $rows | Sort-Object Name -Unique
}

# --------- Main ----------
$rootPath = (Resolve-Path $Root).Path

$slns    = Get-ChildItem -Path $rootPath -Recurse -Include *.sln    -File -ErrorAction SilentlyContinue
$csprojs = Get-ChildItem -Path $rootPath -Recurse -Include *.csproj -File -ErrorAction SilentlyContinue

$repoName = Split-Path $rootPath -Leaf
$now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$sb = New-Object System.Text.StringBuilder
$null = $sb.AppendLine("# .NET Project Report")
$null = $sb.AppendLine()
$null = $sb.AppendLine("Repository: " + $repoName + "  ")
$null = $sb.AppendLine("Generated: " + $now + "  ")
$null = $sb.AppendLine()
$null = $sb.AppendLine("## Overview")
$null = $sb.AppendLine("- Solutions: " + (@($slns).Count))
$null = $sb.AppendLine("- Projects: " + (@($csprojs).Count))
$null = $sb.AppendLine()

if ($slns) {
  $null = $sb.AppendLine("### Solutions")
  foreach ($s in @($slns)) { $null = $sb.AppendLine("- " + $s.FullName) }
  $null = $sb.AppendLine()
}

$null = $sb.AppendLine("## Projects")
foreach ($p in @($csprojs)) {
  $projDir = Split-Path $p.FullName -Parent
  $meta  = Read-CsprojMeta -Path $p.FullName
  $stats = Count-CodeStats -ProjectDir $projDir
  $web   = Detect-WebApiBits -ProjectDir $projDir
  $ef    = Detect-EntityFramework -ProjectDir $projDir
  $pkgs  = Get-Packages -CsprojPath $p.FullName -IncludeTransitive:$IncludeTransitivePackages

  $projFileName = [IO.Path]::GetFileName($p.FullName)
  $null = $sb.AppendLine(("### {0} ({1})" -f $meta.AssemblyName, $projFileName))
  $null = $sb.AppendLine("- Path: " + $p.FullName)
  $null = $sb.AppendLine("- OutputType: " + $meta.OutputType)
  $null = $sb.AppendLine("- TargetFramework(s): " + ((@($meta.TargetFrameworks) -join ", ") -replace '\s+', ' '))
  $null = $sb.AppendLine(("{0}{1}{2}{3}{4}{5}" -f "- Code Stats: ",
                          $stats.Files, " files, ",
                          $stats.LOC, " LOC, ",
                          ($stats.Classes.ToString() + " classes, " + $stats.Interfaces.ToString() + " interfaces, " + $stats.Enums.ToString() + " enums, " + $stats.Records.ToString() + " records")))
  if ( ($web.ControllersCount -gt 0) -or ($web.MinimalApiApprox -gt 0) -or ((@($web.StartupOrProgram).Count) -gt 0) ) {
    $null = $sb.AppendLine("- Web/API Indicators:")
    if ($web.ControllersCount -gt 0) { $null = $sb.AppendLine("  - Controllers: " + $web.ControllersCount) }
    if ($web.MinimalApiApprox -gt 0) { $null = $sb.AppendLine("  - Minimal API endpoints (approx.): " + $web.MinimalApiApprox) }
    if ((@($web.StartupOrProgram).Count) -gt 0) { $null = $sb.AppendLine("  - Startup/Program files: " + (@($web.StartupOrProgram).Count)) }
  }

  if ( ($ef.DbContextCount -gt 0) -or ($ef.HasMigrations) ) {
    $null = $sb.AppendLine("- Entity Framework:")
    if ($ef.DbContextCount -gt 0) { $null = $sb.AppendLine("  - DbContext classes: " + $ef.DbContextCount) }
    if ($ef.HasMigrations)        { $null = $sb.AppendLine("  - Migrations folder present") }
  }

  if ((@($pkgs).Count) -gt 0) {
    $null = $sb.AppendLine("- NuGet Packages:")
    foreach ($pkg in @($pkgs)) { $null = $sb.AppendLine("  - " + $pkg.Name + " " + $pkg.Version) }
  }

  $topDirs = Get-ChildItem -Path $projDir -Directory -ErrorAction SilentlyContinue
  if ($topDirs) { $null = $sb.AppendLine("- Top-Level Folders: " + ((@($topDirs | Select-Object -Expand Name)) -join ", ")) }

  $null = $sb.AppendLine()
}

# Repository-wide stats
$allCs = Get-ChildItem -Path $rootPath -Recurse -Include *.cs -File -ErrorAction SilentlyContinue
$allLoc = 0
foreach ($f in @($allCs)) { 
  $allLoc += ((Get-Content -Raw -Encoding UTF8 $f.FullName) -split "`n").Count 
}

$null = $sb.AppendLine("## Repository Stats")
$null = $sb.AppendLine("- C# files: " + (@($allCs).Count))
$null = $sb.AppendLine("- Total LOC (approx.): " + $allLoc)

$readme = Get-ChildItem -Path $rootPath -Recurse -Include README.md,README.MD,Readme.md -File -ErrorAction SilentlyContinue | Select-Object -First 1
if ($readme) { $null = $sb.AppendLine("- README found: " + $readme.FullName) }
$license = Get-ChildItem -Path $rootPath -Recurse -Include LICENSE,LICENSE.md -File -ErrorAction SilentlyContinue | Select-Object -First 1
if ($license) { $null = $sb.AppendLine("- License found: " + $license.FullName) }

# Write the report
$sb.ToString() | Out-File -FilePath $OutFile -Encoding UTF8

Write-Host ("Report written to {0}" -f $OutFile)
