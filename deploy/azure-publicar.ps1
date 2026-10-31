<#
  Compila a API em Release e publica no App Service criado por azure-provisionar.ps1.
  Uso: powershell -ExecutionPolicy Bypass -File .\deploy\azure-publicar.ps1
#>
$ErrorActionPreference = 'Stop'
$az = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
$config = Get-Content (Join-Path $PSScriptRoot 'azure.json') -Raw | ConvertFrom-Json
$raiz = Split-Path $PSScriptRoot -Parent
$saida = Join-Path $raiz 'artifacts\publish'
$pacote = Join-Path $raiz 'artifacts\api.zip'

Write-Host "1/3 Compilando em Release"
Remove-Item $saida, $pacote -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $raiz 'src\LocadoraCarros.Api') -c Release -o $saida --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Falha no dotnet publish.' }

# ZipFile grava caminhos com '/', exigido pelo App Service Linux (Compress-Archive do PS 5.1 usa '\').
Write-Host "2/3 Gerando pacote"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($saida, $pacote)

Write-Host "3/3 Enviando para $($config.app)"
& $az webapp deploy -g $config.grupoRecursos -n $config.app --src-path $pacote --type zip --only-show-errors -o none
if ($LASTEXITCODE -ne 0) { throw 'Falha no az webapp deploy.' }

Write-Host ""
Write-Host "Publicado: https://$($config.app).azurewebsites.net/scalar/v1"
Write-Host "No plano gratuito a primeira chamada depois de um tempo parado pode levar alguns segundos."
