param([Parameter(Mandatory=$true)][string]$SourceProject,[Parameter(Mandatory=$true)][string]$OutputFolder)
$ErrorActionPreference='Stop'
if(Get-Process Revit -ErrorAction SilentlyContinue){throw 'Feche o Revit normalmente antes de preparar o job.'}
$packageRoot=Split-Path $PSScriptRoot -Parent
$job=Join-Path $packageRoot 'current\section-validation-job.json'
if(Test-Path -LiteralPath $job){throw 'Já há um job pendente.'}
$source=(Resolve-Path -LiteralPath $SourceProject).Path
if([IO.Path]::GetExtension($source) -ne '.rvt'){throw 'A origem deve ser um RVT.'}
$folder=[IO.Path]::GetFullPath($OutputFolder)
$destination=Join-Path $folder 'CASA_TERRAS_SO_MODELAGEM.rvt'
if(Test-Path -LiteralPath $destination){throw 'A entrega já existe; escolha uma pasta nova.'}
New-Item -ItemType Directory -Path $folder -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $destination
@{Project=$destination;Output=$folder;Mode='modelreview'} | ConvertTo-Json | Set-Content -LiteralPath $job -Encoding utf8
Write-Output "Abra o Revit para processar apenas a cópia: $destination"
