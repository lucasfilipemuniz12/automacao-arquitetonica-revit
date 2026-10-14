param(
 [Parameter(Mandatory=$true)][string]$SourceProject,
 [Parameter(Mandatory=$true)][string]$OutputFolder,
 [ValidateSet('sections','legal05','composition014')][string]$Mode='legal05'
)
$ErrorActionPreference='Stop'
$packageRoot=Split-Path $PSScriptRoot -Parent
$sourceFile=(Resolve-Path -LiteralPath $SourceProject).Path
if([IO.Path]::GetExtension($sourceFile) -ne '.rvt'){throw 'A origem precisa ser um RVT local de teste.'}
$candidateDll=Join-Path $packageRoot 'current\Vebram.Revit.dll'
if(!(Test-Path -LiteralPath $candidateDll)){throw 'Compile e instale o produto antes da validação.'}
if((Get-Process Revit -ErrorAction SilentlyContinue | Where-Object {$_.MainWindowHandle -ne 0})){throw 'Feche as janelas do Revit antes de preparar a validação de inicialização.'}
$fileLock=[IO.File]::Open($candidateDll,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$fileLock.Dispose()
$jobPath=Join-Path $packageRoot 'current\section-validation-job.json'
if(Test-Path -LiteralPath $jobPath){throw 'Já existe um job de validação pendente. Confira-o antes de preparar outro.'}
$destinationFolder=[IO.Path]::GetFullPath($OutputFolder)
$targetFile=Join-Path $destinationFolder ('VALIDACAO_'+$Mode+'_'+[DateTime]::Now.ToString('yyyyMMdd_HHmmss')+'.rvt')
if($targetFile -eq $sourceFile -or (Test-Path -LiteralPath $targetFile)){throw 'A validação exige uma cópia nova e não substitui a origem.'}
New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
Copy-Item -LiteralPath $sourceFile -Destination $targetFile
@{Project=$targetFile;Output=$destinationFolder;Mode=$Mode} | ConvertTo-Json | Set-Content -LiteralPath $jobPath -Encoding utf8
Write-Output "Job preparado: $jobPath"
Write-Output "Abra o Revit para validar somente a cópia: $targetFile"
if($Mode -eq 'composition014'){Write-Output 'composition014 verifica a revisão explícita de composição V02, margens, escalas, repetição e preservação da geometria numa cópia com entregáveis já preparados e colocados.'}
else{Write-Output 'legal05 usa dados fictícios explicitamente identificados. Não verifica conformidade municipal.'}
Write-Output 'Sem job explícito, o produto não executa testes na inicialização.'
