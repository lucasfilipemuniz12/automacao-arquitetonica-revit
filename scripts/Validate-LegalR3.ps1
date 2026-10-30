param(
 [Parameter(Mandatory=$true)][string]$CasaProject,
 [Parameter(Mandatory=$true)][string]$SobradoProject,
 [Parameter(Mandatory=$true)][string]$CasaData,
 [Parameter(Mandatory=$true)][string]$OutputFolder,
 [string]$Casa04Project
)
$ErrorActionPreference='Stop'
if(Get-Process Revit -ErrorAction SilentlyContinue){throw 'Salve os documentos e feche todas as sessões do Revit antes de preparar a rodada.'}
$package=Split-Path -Parent $PSScriptRoot
$job=Join-Path $package 'current\section-validation-job.json'
if(Test-Path -LiteralPath $job){throw 'Já existe uma rodada pendente.'}
$output=[IO.Path]::GetFullPath($OutputFolder)
if(Test-Path -LiteralPath $output){throw 'Escolha pasta nova para preservar evidências anteriores.'}
New-Item -ItemType Directory -Path (Join-Path $output 'CT03') -Force|Out-Null
$sources=@((Resolve-Path -LiteralPath $CasaProject).Path,(Resolve-Path -LiteralPath $SobradoProject).Path)
if($Casa04Project){$sources+=(Resolve-Path -LiteralPath $Casa04Project).Path}
foreach($p in $sources){if([IO.Path]::GetExtension($p) -ne '.rvt'){throw 'Testes aceitam somente RVT; nunca RTE.'}}
$copies=@('R3_TESTE_CT03.rvt','R3_TESTE_Sobrado.rvt','R3_TESTE_CT04.rvt')
for($i=0;$i -lt $sources.Count;$i++){Copy-Item -LiteralPath $sources[$i] -Destination (Join-Path $output $copies[$i])}
Copy-Item -LiteralPath $CasaData -Destination (Join-Path $output 'CT03\casa.json')
$data=@{Project=(Join-Path $output $copies[0]);Reference=(Join-Path $output $copies[1]);Output=$output;Mode='legalr3'}
if($Casa04Project){$data.Case04=Join-Path $output $copies[2]}
$data|ConvertTo-Json|Set-Content -LiteralPath $job -Encoding utf8
Get-FileHash -LiteralPath $sources |Select-Object Path,Hash|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $output 'ORIGINAIS_SHA256.json')
Write-Output "Rodada preparada em cópias: $output"
