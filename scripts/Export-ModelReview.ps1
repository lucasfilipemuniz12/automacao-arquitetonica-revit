param([Parameter(Mandatory=$true)][string]$SourceProject,[Parameter(Mandatory=$true)][string]$Template,[Parameter(Mandatory=$true)][string]$ReferenceProject,[Parameter(Mandatory=$true)][string]$OutputFolder)
$ErrorActionPreference='Stop'
if(Get-Process Revit -ErrorAction SilentlyContinue){throw 'Feche o Revit normalmente antes de preparar o job.'}
$packageRoot=Split-Path $PSScriptRoot -Parent
$job=Join-Path $packageRoot 'current\section-validation-job.json'
if(Test-Path -LiteralPath $job){throw 'Já há um job pendente.'}
$source=(Resolve-Path -LiteralPath $SourceProject).Path
$templateFile=(Resolve-Path -LiteralPath $Template).Path
$referenceFile=(Resolve-Path -LiteralPath $ReferenceProject).Path
if([IO.Path]::GetExtension($source) -ne '.rvt' -or [IO.Path]::GetExtension($templateFile) -ne '.rte'){throw 'Informe a base RVT anterior à automação e o RTE original.'}
$folder=[IO.Path]::GetFullPath($OutputFolder)
$destination=Join-Path $folder 'CASA_TERRAS_BASE_TEMPLATE_V02.rvt'
if(Test-Path -LiteralPath $destination){throw 'A entrega já existe; escolha uma pasta nova.'}
New-Item -ItemType Directory -Path $folder -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $destination
@{Project=$destination;Output=$folder;Mode='templatereview';Template=$templateFile;Reference=$referenceFile} | ConvertTo-Json | Set-Content -LiteralPath $job -Encoding utf8
Write-Output "Abra o Revit para verificar apenas a cópia: $destination"
Write-Output "O job cria CASA_TERRAS_TEMPLATE_V02_PARA_AUTOMACAO.rvt diretamente do RTE, transfere apenas a modelagem e preserva as vistas originais."
