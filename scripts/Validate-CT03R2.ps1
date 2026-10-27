param(
 [Parameter(Mandatory=$true)][string]$CasaProject,
 [Parameter(Mandatory=$true)][string]$SobradoProject,
 [Parameter(Mandatory=$true)][string]$CasaData,
 [Parameter(Mandatory=$true)][string]$OutputFolder
)
$ErrorActionPreference='Stop'
if(Get-Process Revit -ErrorAction SilentlyContinue){throw 'Feche o Revit após salvar os documentos antes de preparar a rodada.'}
$package=Split-Path -Parent $PSScriptRoot
$job=Join-Path $package 'current\section-validation-job.json'
if(Test-Path -LiteralPath $job){throw 'Já existe uma validação pendente; preserve-a antes de preparar outra.'}
if(!(Test-Path -LiteralPath (Join-Path $package 'current\Vebram.Revit.dll'))){throw 'Compile e instale o plugin antes desta rodada.'}
$casa=(Resolve-Path -LiteralPath $CasaProject).Path
$sobrado=(Resolve-Path -LiteralPath $SobradoProject).Path
$fixture=(Resolve-Path -LiteralPath $CasaData).Path
foreach($model in @($casa,$sobrado)){if([IO.Path]::GetExtension($model) -ne '.rvt'){throw 'As duas origens devem ser RVT; templates não são aceitos.'}}
$output=[IO.Path]::GetFullPath($OutputFolder)
if(Test-Path -LiteralPath $output){throw 'Escolha uma pasta de saída nova para preservar rodadas anteriores.'}
New-Item -ItemType Directory -Path (Join-Path $output 'CT03') -Force | Out-Null
$casaCopy=Join-Path $output 'R2_TESTE_CT03.rvt'
$sobradoCopy=Join-Path $output 'R2_TESTE_Sobrado.rvt'
Copy-Item -LiteralPath $casa -Destination $casaCopy
Copy-Item -LiteralPath $sobrado -Destination $sobradoCopy
Copy-Item -LiteralPath $fixture -Destination (Join-Path $output 'CT03\casa.json')
@{Project=$casaCopy;Reference=$sobradoCopy;Output=$output;Mode='ct03r2'} | ConvertTo-Json | Set-Content -LiteralPath $job -Encoding utf8
Get-FileHash -LiteralPath $casa,$sobrado | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'ORIGINAIS_SHA256.json') -Encoding utf8
Write-Output "Rodada preparada em cópias novas: $output. Abra o Revit e autorize o complemento."
