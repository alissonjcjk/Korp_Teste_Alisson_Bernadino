[CmdletBinding()]
param(
    [switch]$SkipRestore
)

$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskFrontendPath = Join-Path $taskRepositoryRoot "frontend\korp-erp-frontend"

function Invoke-TaskCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Description,

        [Parameter(Mandatory)]
        [scriptblock]$Action
    )

    Write-Host "`n==> $Description"
    & $Action

    if ($LASTEXITCODE -ne 0) {
        throw "$Description falhou com o código de saída $LASTEXITCODE."
    }
}

Push-Location $taskRepositoryRoot

try {
    if (-not $SkipRestore) {
        Invoke-TaskCommand "Restaurando dependências .NET" {
            dotnet restore Korp.Erp.slnx
        }
    }

    Invoke-TaskCommand "Compilando backend e testes em Release" {
        dotnet build Korp.Erp.slnx --configuration Release --no-restore
    }

    Invoke-TaskCommand "Executando testes dos microsserviços" {
        dotnet test Korp.Erp.slnx --configuration Release --no-build
    }

    Push-Location $taskFrontendPath

    try {
        if (-not $SkipRestore) {
            Invoke-TaskCommand "Instalando dependências exatas do frontend" {
                npm ci
            }
        }

        Invoke-TaskCommand "Compilando frontend em produção" {
            npm run build
        }

        Invoke-TaskCommand "Executando testes Angular sem modo interativo" {
            npm run test:ci
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "`nVerificação da Etapa 1 concluída com sucesso."
}
finally {
    Pop-Location
}
