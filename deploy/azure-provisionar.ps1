<#
  Cria a infraestrutura gratuita da API na Azure:
    - App Service no plano F1 (Free), Linux, .NET 10
    - Azure SQL Database com a oferta gratuita, que PAUSA ao esgotar a cota mensal (sem cobranca)
  Segredos (senha do SQL, chave JWT e senha dos usuarios de teste) sao gerados aqui e gravados
  direto nas configuracoes do App Service. Nenhum segredo e salvo em arquivo.
  Pode ser executado de novo: reaproveita os nomes de deploy/azure.json e os segredos ja configurados.

  Uso: powershell -ExecutionPolicy Bypass -File .\deploy\azure-provisionar.ps1
#>
param(
    [string]$Local = 'brazilsouth',
    [string]$GrupoRecursos = 'rg-locadora-carros'
)

$ErrorActionPreference = 'Stop'
$az = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
$arquivoConfig = Join-Path $PSScriptRoot 'azure.json'

function Invoke-Az {
    $saida = & $az @args --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "Falha no comando: az $($args[0]) $($args[1]) $($args[2])" }
    $saida
}

function Test-Az {
    # No PowerShell 5.1, stderr de comando nativo com 'Stop' vira excecao; aqui o erro e esperado.
    $ErrorActionPreference = 'Continue'
    & $az @args --only-show-errors -o none 2>$null
    $LASTEXITCODE -eq 0
}

function New-Segredo([int]$tamanho) {
    $caracteres = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
    $bytes = New-Object byte[] $tamanho
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    -join ($bytes | ForEach-Object { $caracteres[$_ % $caracteres.Length] })
}

# Nomes globais (App Service e SQL Server) precisam ser unicos na Azure: usamos um sufixo aleatorio.
if (Test-Path $arquivoConfig) {
    $config = Get-Content $arquivoConfig -Raw | ConvertFrom-Json
} else {
    $sufixo = (New-Segredo 5).ToLowerInvariant()
    $config = [pscustomobject]@{
        grupoRecursos = $GrupoRecursos
        local         = $Local
        plano         = 'plan-locadora-carros'
        app           = "locadora-carros-api-$sufixo"
        sqlServer     = "locadora-carros-sql-$sufixo"
        banco         = 'locadora_carros'
        sqlAdmin      = 'locadoraadmin'
    }
    $config | ConvertTo-Json | Set-Content $arquivoConfig -Encoding ASCII
}

Write-Host "1/6 Grupo de recursos $($config.grupoRecursos) em $($config.local)"
Invoke-Az group create -n $config.grupoRecursos -l $config.local -o none

Write-Host "2/6 Plano App Service F1 (gratuito, Linux)"
Invoke-Az appservice plan create -g $config.grupoRecursos -n $config.plano -l $config.local --sku F1 --is-linux -o none

Write-Host "3/6 Web App $($config.app) (.NET 10)"
if (-not (Test-Az webapp show -g $config.grupoRecursos -n $config.app)) {
    Invoke-Az webapp create -g $config.grupoRecursos -p $config.plano -n $config.app --runtime 'DOTNETCORE:10.0' -o none
}
Invoke-Az webapp update -g $config.grupoRecursos -n $config.app --https-only true -o none
Invoke-Az webapp config set -g $config.grupoRecursos -n $config.app --startup-file 'dotnet LocadoraCarros.Api.dll' --ftps-state Disabled -o none

Write-Host "4/6 Servidor Azure SQL $($config.sqlServer)"
$senhaSql = (New-Segredo 22) + 'a7K'
if (Test-Az sql server show -g $config.grupoRecursos -n $config.sqlServer) {
    Invoke-Az sql server update -g $config.grupoRecursos -n $config.sqlServer -p $senhaSql -o none
} else {
    Invoke-Az sql server create -g $config.grupoRecursos -n $config.sqlServer -l $config.local -u $config.sqlAdmin -p $senhaSql -o none
}
# Libera apenas servicos da propria Azure (o App Service); nenhum IP externo tem acesso.
Invoke-Az sql server firewall-rule create -g $config.grupoRecursos -s $config.sqlServer -n PermitirServicosAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none

Write-Host "5/6 Banco $($config.banco) com a oferta gratuita (pausa ao esgotar a cota)"
if (-not (Test-Az sql db show -g $config.grupoRecursos -s $config.sqlServer -n $config.banco)) {
    Invoke-Az sql db create -g $config.grupoRecursos -s $config.sqlServer -n $config.banco `
        -e GeneralPurpose --compute-model Serverless -f Gen5 -c 2 `
        --use-free-limit --free-limit-exhaustion-behavior AutoPause --backup-storage-redundancy Local -o none
}

Write-Host "6/6 Configuracoes da aplicacao"
$existentes = @{}
(Invoke-Az webapp config appsettings list -g $config.grupoRecursos -n $config.app -o json | Out-String | ConvertFrom-Json) |
    ForEach-Object { $existentes[$_.name] = $_.value }

$chaveJwt = if ($existentes['Jwt__ChaveSecreta']) { $existentes['Jwt__ChaveSecreta'] } else { New-Segredo 64 }
$senhaSeed = if ($existentes['Seed__SenhaPadrao']) { $existentes['Seed__SenhaPadrao'] } else { (New-Segredo 10) + '7k' }
$conexao = "Server=tcp:$($config.sqlServer).database.windows.net,1433;Initial Catalog=$($config.banco);User ID=$($config.sqlAdmin);Password=$senhaSql;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"

Invoke-Az webapp config appsettings set -g $config.grupoRecursos -n $config.app -o none --settings `
    "Database__Provider=SqlServer" `
    "ConnectionStrings__Locadora=$conexao" `
    "Jwt__ChaveSecreta=$chaveJwt" `
    "Seed__Habilitado=true" `
    "Seed__SenhaPadrao=$senhaSeed" `
    "Documentacao__Habilitada=true" `
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"

Write-Host ""
Write-Host "Infraestrutura pronta. Publique com: powershell -ExecutionPolicy Bypass -File .\deploy\azure-publicar.ps1"
Write-Host "API:          https://$($config.app).azurewebsites.net/api"
Write-Host "Documentacao: https://$($config.app).azurewebsites.net/scalar/v1"
Write-Host "Senha dos usuarios de teste (gerente, funcionario, cliente @locadora.dev): $senhaSeed"
