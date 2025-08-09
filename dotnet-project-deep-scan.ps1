<# 
dotnet-project-deep-scan.ps1  (PowerShell 5.1 compatible)
Outputs a deep code map of controllers, actions, routes, models, services, DbContext/DbSets, areas, views, migrations.
Produces CodeMap.md and CodeMap.json. ASCII only. No self-invocation.
#>

param(
  [string]$Root = ".",
  [string]$OutMd = "CodeMap.md",
  [string]$OutJson = "CodeMap.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-Text($path) {
  try { return (Get-Content -Raw -Encoding UTF8 $path) } catch { return "" }
}

function Trim-NonEmpty($s) { if ($s) { return $s.Trim() } else { return "" } }

function Parse-Namespace($text) {
  $m = [regex]::Match($text, '^\s*namespace\s+([A-Za-z0-9_\.]+)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
  if ($m.Success) { return $m.Groups[1].Value } else { return "" }
}

function Parse-Controller($path) {
  $text = Read-Text $path
  $ns = Parse-Namespace $text

  # Controller class
  $classMatch = [regex]::Match($text, 'class\s+([A-Za-z0-9_]+)\s*(?::\s*([A-Za-z0-9_<>,\s]+))?')
  $className = ""; $baseType = ""
  if ($classMatch.Success) {
    $className = $classMatch.Groups[1].Value
    $baseType  = Trim-NonEmpty $classMatch.Groups[2].Value
  }

  # Controller-level attributes
  $ctrlAttrs = [regex]::Matches($text, '^\s*\[(?<attr>[A-Za-z0-9_]+)(\((?<args>[^\)]*)\))?\]\s*$(\s*^(?:public|internal|sealed|abstract).*\bclass\b)', 
    [System.Text.RegularExpressions.RegexOptions]::Multiline)
  $controllerRoute = ""
  $isApiController = $false
  $isAuthorized = $false
  foreach ($a in $ctrlAttrs) {
    $n = $a.Groups["attr"].Value
    $args = $a.Groups["args"].Value
    if ($n -eq "Route" -and $args) {
      $m = [regex]::Match($args, '"([^"]*)"')
      if ($m.Success) { $controllerRoute = $m.Groups[1].Value }
    }
    if ($n -eq "ApiController") { $isApiController = $true }
    if ($n -eq "Authorize") { $isAuthorized = $true }
  }

  # Actions
  $lines = ($text -split "`n")
  $actions = @()
  for ($i=0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]

    # Gather preceding attributes
    $attrs = @()
    $j = $i
    while ($j -lt $lines.Count -and $lines[$j].Trim().StartsWith("[")) {
      $attrs += $lines[$j].Trim()
      $j++
      if ($j -ge $lines.Count) { break }
    }

    # Method signature
    $sig = $lines[$j]
    if ($sig -match '^\s*public\s+(?:async\s+)?([A-Za-z0-9_<>\[\],\s]+)\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)\s*') {
      $returnType = ($matches[1]).Trim()
      $methodName = ($matches[2]).Trim()
      $paramSig   = ($matches[3]).Trim()

      # Extract HTTP verb + route from attributes
      $httpVerb = ""; $methodRoute = ""
      foreach ($a in $attrs) {
        $am = [regex]::Match($a, '^\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch|HttpHead|HttpOptions)(\((?<args>[^\)]*)\))?\]$', 
          [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($am.Success) {
          $httpVerb = $am.Groups[1].Value.ToUpper()
          $args = $am.Groups["args"].Value
          if ($args) {
            $rm = [regex]::Match($args, '"([^"]*)"')
            if ($rm.Success) { $methodRoute = $rm.Groups[1].Value }
          }
        }
        if (-not $httpVerb -and $a -match '^\[Route\((?<args>[^\)]*)\)\]$') {
          $rargs = [regex]::Match($a, '"([^"]*)"')
          if ($rargs.Success) { $methodRoute = $rargs.Groups[1].Value }
        }
      }

      $actions += [pscustomobject]@{
        Name       = $methodName
        ReturnType = $returnType
        Parameters = $paramSig
        HttpVerb   = $httpVerb
        Route      = $methodRoute
        Attributes = $attrs
        Line       = ($j+1)
      }
      $i = $j
    }
  }

  return [pscustomobject]@{
    FilePath         = $path
    Namespace        = $ns
    Class            = $className
    BaseType         = $baseType
    ControllerRoute  = $controllerRoute
    ApiController    = $isApiController
    Authorize        = $isAuthorized
    Actions          = $actions
  }
}

function Parse-DbContext($path) {
  $text = Read-Text $path
  $ns = Parse-Namespace $text
  $m = [regex]::Match($text, 'class\s+([A-Za-z0-9_]*DbContext)\s*:\s*DbContext')
  if (-not $m.Success) { return $null }
  $ctxName = $m.Groups[1].Value

  $dbsets = @()
  foreach ($mm in [regex]::Matches($text, 'DbSet<\s*([A-Za-z0-9_\.]+)\s*>\s+([A-Za-z0-9_]+)\s*\{', 
           [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
    $dbsets += [pscustomobject]@{
      EntityType = $mm.Groups[1].Value
      Property   = $mm.Groups[2].Value
    }
  }

  return [pscustomobject]@{
    FilePath   = $path
    Namespace  = $ns
    Class      = $ctxName
    DbSets     = $dbsets
  }
}

function Parse-Interfaces($path) {
  $text = Read-Text $path
  $list = @()
  foreach ($m in [regex]::Matches($text, 'interface\s+(I[A-Za-z0-9_]+)', 
           [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
    $list += $m.Groups[1].Value
  }
  return $list
}

function Parse-Classes($path) {
  $text = Read-Text $path
  $list = @()
  foreach ($m in [regex]::Matches($text, 'class\s+([A-Za-z0-9_]+)\b', 
           [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
    $list += $m.Groups[1].Value
  }
  return $list
}

function Scan {
  param([string]$root)

  $rootPath = (Resolve-Path $root).Path

  # Controllers (including Areas/*/Controllers)
  $controllerFiles = Get-ChildItem -Path $rootPath -Recurse -Include *Controller.cs -File -ErrorAction SilentlyContinue
  $controllers = @()
  foreach ($f in @($controllerFiles)) { $controllers += (Parse-Controller $f.FullName) }

  # DbContext(s)
  $csFiles = Get-ChildItem -Path $rootPath -Recurse -Include *.cs -File -ErrorAction SilentlyContinue
  $dbcontexts = @()
  foreach ($f in @($csFiles)) {
    $ctx = Parse-DbContext $f.FullName
    if ($ctx) { $dbcontexts += $ctx }
  }

  # Models (prefer Models folder)
  $modelFiles = Get-ChildItem -Path $rootPath -Recurse -Include *.cs -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\Models\\' -and $_.Name -notmatch 'Controller\.cs$' }
  $modelClasses = @()
  foreach ($mf in @($modelFiles)) { $modelClasses += (Parse-Classes $mf.FullName) }
  $modelClasses = ($modelClasses | Sort-Object -Unique)

  # Services (prefer Services folder)
  $serviceFiles = Get-ChildItem -Path $rootPath -Recurse -Include *.cs -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\Services\\' -and $_.Name -notmatch 'Controller\.cs$' }
  $serviceInterfaces = @()
  $serviceClasses = @()
  foreach ($sf in @($serviceFiles)) {
    $serviceInterfaces += (Parse-Interfaces $sf.FullName)
    $serviceClasses   += (Parse-Classes $sf.FullName)
  }
  $serviceInterfaces = ($serviceInterfaces | Where-Object { $_ -match '^I.*Service$' } | Sort-Object -Unique)
  $serviceClasses    = ($serviceClasses    | Where-Object { $_ -match '.*Service$' }   | Sort-Object -Unique)

  # DI registrations in Program.cs or Startup.cs (best effort)
  $startupFiles = Get-ChildItem -Path $rootPath -Recurse -Include Program.cs,Startup.cs -File -ErrorAction SilentlyContinue
  $registrations = @()
  foreach ($sf in @($startupFiles)) {
    $txt = Read-Text $sf.FullName
    foreach ($m in [regex]::Matches($txt, 'Add(Singleton|Scoped|Transient)\s*<\s*([A-Za-z0-9_\.]+)\s*,\s*([A-Za-z0-9_\.]+)\s*>', 
             [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
      $registrations += [pscustomobject]@{
        Lifetime = $m.Groups[1].Value
        Interface = $m.Groups[2].Value
        Implementation = $m.Groups[3].Value
        FilePath = $sf.FullName
      }
    }
  }

  # Areas
  $areasDir = Join-Path $rootPath "Areas"
  $areas = @()
  if (Test-Path $areasDir) {
    $areaFolders = Get-ChildItem -Path $areasDir -Directory -ErrorAction SilentlyContinue
    foreach ($a in @($areaFolders)) {
      $areaControllers = Get-ChildItem -Path (Join-Path $a.FullName "Controllers") -Recurse -Include *Controller.cs -File -ErrorAction SilentlyContinue
      $parsed = @()
      foreach ($f in @($areaControllers)) { $parsed += (Parse-Controller $f.FullName) }
      $areas += [pscustomobject]@{
        Area = $a.Name
        Controllers = $parsed
      }
    }
  }

  # Views summary
  $views = @()
  $viewRoot = Join-Path $rootPath "Views"
  if (Test-Path $viewRoot) {
    $viewDirs = Get-ChildItem -Path $viewRoot -Directory -ErrorAction SilentlyContinue
    foreach ($vd in @($viewDirs)) {
      $cshtml = Get-ChildItem -Path $vd.FullName -Recurse -Include *.cshtml -File -ErrorAction SilentlyContinue
      $views += [pscustomobject]@{
        ControllerOrFolder = $vd.Name
        Count = (@($cshtml).Count)
      }
    }
  }
  # Area views
  if (Test-Path $areasDir) {
    $areaViewDirs = Get-ChildItem -Path $areasDir -Recurse -Directory -Filter Views -ErrorAction SilentlyContinue
    foreach ($vd in @($areaViewDirs)) {
      $cshtml = Get-ChildItem -Path $vd.FullName -Recurse -Include *.cshtml -File -ErrorAction SilentlyContinue
      $views += [pscustomobject]@{
        ControllerOrFolder = ("Area:" + (Split-Path (Split-Path $vd.FullName -Parent) -Leaf))
        Count = (@($cshtml).Count)
      }
    }
  }

  # Migrations
  $migrationsDir = Get-ChildItem -Path $rootPath -Recurse -Directory -Filter "Migrations" -ErrorAction SilentlyContinue | Select-Object -First 1
  $migrations = @()
  if ($migrationsDir) {
    $migFiles = Get-ChildItem -Path $migrationsDir.FullName -File -ErrorAction SilentlyContinue
    $latest = $null
    if ($migFiles) { $latest = ($migFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1) }
    $latestName = ""
    if ($latest) { $latestName = $latest.Name }
    $migrations = [pscustomobject]@{
      Path = $migrationsDir.FullName
      Count = (@($migFiles).Count)
      Latest = $latestName
    }
  }

  # Build result object
  return [pscustomobject]@{
    RootPath        = $rootPath
    Controllers     = $controllers
    Areas           = $areas
    DbContexts      = $dbcontexts
    Models          = $modelClasses
    Services        = [pscustomobject]@{
                        Interfaces = $serviceInterfaces
                        Classes    = $serviceClasses
                        Registrations = $registrations
                      }
    Views           = $views
    Migrations      = $migrations
    GeneratedAt     = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
  }
}

# ---- Run scan and write outputs ----
$map = Scan -root $Root

# JSON
($map | ConvertTo-Json -Depth 8) | Out-File -FilePath $OutJson -Encoding UTF8

# Markdown
$md = New-Object System.Text.StringBuilder
$null = $md.AppendLine("# Code Map")
$null = $md.AppendLine()
$null = $md.AppendLine("Root: " + $map.RootPath)
$null = $md.AppendLine("Generated: " + $map.GeneratedAt)
$null = $md.AppendLine()

# Controllers
$null = $md.AppendLine("## Controllers")
foreach ($c in @($map.Controllers)) {
  $null = $md.AppendLine("- " + $c.Class + "  (" + $c.FilePath + ")")
  if ($c.ControllerRoute) { $null = $md.AppendLine("  - Route: " + $c.ControllerRoute) }
  if ($c.ApiController)   { $null = $md.AppendLine("  - ApiController: true") }
  if ($c.Authorize)       { $null = $md.AppendLine("  - Authorize: true") }
  if ((@($c.Actions).Count) -gt 0) {
    $null = $md.AppendLine("  - Actions:")
    foreach ($a in @($c.Actions)) {
      $verb = ""; if ($a.HttpVerb) { $verb = $a.HttpVerb }
      $route = ""; if ($a.Route) { $route = $a.Route }
      $null = $md.AppendLine("    - " + $a.Name + "  [" + $verb + "]  (" + $route + ")  -> " + $a.ReturnType)
    }
  }
}

# Areas
if ((@($map.Areas).Count) -gt 0) {
  $null = $md.AppendLine()
  $null = $md.AppendLine("## Areas")
  foreach ($ar in @($map.Areas)) {
    $null = $md.AppendLine("- Area: " + $ar.Area)
    foreach ($c in @($ar.Controllers)) {
      $null = $md.AppendLine("  - " + $c.Class + "  (" + $c.FilePath + ")")
      if ((@($c.Actions).Count) -gt 0) {
        foreach ($a in @($c.Actions)) {
          $verb = ""; if ($a.HttpVerb) { $verb = $a.HttpVerb }
          $route = ""; if ($a.Route) { $route = $a.Route }
          $null = $md.AppendLine("    - " + $a.Name + "  [" + $verb + "]  (" + $route + ")  -> " + $a.ReturnType)
        }
      }
    }
  }
}

# DbContexts
if ((@($map.DbContexts).Count) -gt 0) {
  $null = $md.AppendLine()
  $null = $md.AppendLine("## DbContexts and DbSets")
  foreach ($d in @($map.DbContexts)) {
    $null = $md.AppendLine("- " + $d.Class + "  (" + $d.FilePath + ")")
    foreach ($ds in @($d.DbSets)) {
      $null = $md.AppendLine("  - DbSet: " + $ds.Property + "  : " + $ds.EntityType)
    }
  }
}

# Models
if ((@($map.Models).Count) -gt 0) {
  $null = $md.AppendLine()
  $null = $md.AppendLine("## Models")
  foreach ($m in @($map.Models)) { $null = $md.AppendLine("- " + $m) }
}

# Services
$null = $md.AppendLine()
$null = $md.AppendLine("## Services")
if ((@($map.Services.Interfaces).Count) -gt 0) {
  $null = $md.AppendLine("- Interfaces:")
  foreach ($i in @($map.Services.Interfaces)) { $null = $md.AppendLine("  - " + $i) }
}
if ((@($map.Services.Classes).Count) -gt 0) {
  $null = $md.AppendLine("- Implementations:")
  foreach ($s in @($map.Services.Classes)) { $null = $md.AppendLine("  - " + $s) }
}
if ((@($map.Services.Registrations).Count) -gt 0) {
  $null = $md.AppendLine("- DI Registrations:")
  foreach ($r in @($map.Services.Registrations)) {
    $null = $md.AppendLine("  - " + $r.Lifetime + "  " + $r.Interface + " -> " + $r.Implementation + "  (" + $r.FilePath + ")")
  }
}

# Views
if ((@($map.Views).Count) -gt 0) {
  $null = $md.AppendLine()
  $null = $md.AppendLine("## Views")
  foreach ($v in @($map.Views)) {
    $null = $md.AppendLine("- " + $v.ControllerOrFolder + " : " + $v.Count)
  }
}

# Migrations
if ($map.Migrations -and $map.Migrations.Path) {
  $null = $md.AppendLine()
  $null = $md.AppendLine("## Migrations")
  $null = $md.AppendLine("- Path: " + $map.Migrations.Path)
  $null = $md.AppendLine("- Count: " + $map.Migrations.Count)
  $null = $md.AppendLine("- Latest: " + $map.Migrations.Latest)
}

$md.ToString() | Out-File -FilePath $OutMd -Encoding UTF8

Write-Host ("Wrote {0} and {1}" -f $OutMd, $OutJson)
