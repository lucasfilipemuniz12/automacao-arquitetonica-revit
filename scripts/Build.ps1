param([string]$Dotnet = 'dotnet', [string]$RevitInstallDir = 'D:\Autodesk\Revit2026\Revit 2026', [string]$DistName = 'current')
$ErrorActionPreference = 'Stop'
if (Get-Process -Name Revit -ErrorAction SilentlyContinue) { throw 'Feche o Revit antes de compilar/atualizar a instalação fixa. Isso evita DLL bloqueada e carregadores temporários.' }
$package = Split-Path -Parent $PSScriptRoot
$project = Join-Path $package 'src\Vebram.Revit\Vebram.Revit.csproj'
$logs = Join-Path $package 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$binlog = '-bl:' + (Join-Path $logs 'build-{}.binlog')
& $Dotnet build $project -c Release --configfile (Join-Path $package 'NuGet.Config') "-p:RevitInstallDir=$RevitInstallDir" -p:NuGetAudit=false $binlog
if ($LASTEXITCODE -ne 0) { throw 'Falha de compilação. Consulte o binlog local.' }
$output = Join-Path $package 'src\Vebram.Revit\bin\Release\net8.0-windows'
$dist = Join-Path $package $DistName
New-Item -ItemType Directory -Path $dist -Force | Out-Null
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$assemblyName = $projectXml.Project.PropertyGroup.AssemblyName
foreach ($extension in @('.dll','.pdb','.deps.json')) {
    $name = $assemblyName + $extension
    Copy-Item -LiteralPath (Join-Path $output $name) -Destination (Join-Path $dist $name)
}
New-Item -ItemType Directory -Path (Join-Path $dist 'profiles') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $output 'profiles\ARQ_Parametros_Compartilhados.txt') -Destination (Join-Path $dist 'profiles\ARQ_Parametros_Compartilhados.txt')
Write-Output "Pacote compilado: $dist"






