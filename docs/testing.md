# Testes e baseline reproduzível

Este documento descreve o portão de qualidade iniciado na Etapa 1 e ampliado na
Etapa 2. Além do baseline, a suíte agora protege o contrato de erros, model
binding, validações, códigos HTTP e consumo desse contrato pelo Angular.

## Verificação completa

Na raiz do repositório, execute:

```powershell
pwsh -File .\scripts\verify-stage1.ps1
```

O script restaura as dependências a partir dos arquivos de projeto e do
`package-lock.json`, compila os dois microsserviços e o Angular em modo de
produção e executa todas as suítes sem modo interativo.

Depois da primeira execução, uma verificação mais rápida pode reutilizar as
dependências já restauradas:

```powershell
pwsh -File .\scripts\verify-stage1.ps1 -SkipRestore
```

Para validar especificamente o estado após a Etapa 2, incluindo tipos e
templates Angular, use:

```powershell
pwsh -File .\scripts\verify-stage2.ps1 -SkipRestore
```

A opção `-VerifyDotNet9Container` repete as suítes backend na imagem oficial do
SDK .NET 9, sem escrever no repositório.

Qualquer comando com código de saída diferente de zero interrompe a verificação.

## Comandos separados

Backend:

```powershell
dotnet restore .\Korp.Erp.slnx
dotnet build .\Korp.Erp.slnx --configuration Release --no-restore
dotnet test .\Korp.Erp.slnx --configuration Release --no-build
```

Frontend:

```powershell
Set-Location .\frontend\korp-erp-frontend
npm ci
npm run build
npm run test:ci
```

## Escopo da suíte

- **InventoryService:** validadores, contratos do modelo EF Core e operações de
  cadastro, consulta, atualização, baixa e exclusão de produtos.
- **BillingService:** validadores, cálculos e mapeamentos de nota, criação e
  consulta de notas, regras atuais de impressão, idempotência serial e conteúdo
  do evento publicado.
- **Angular:** shell da aplicação, formulários de produto e nota e contratos HTTP
  dos serviços de produto e impressão.

Os testes dos serviços usam o provedor EF Core em memória, e o endpoint de
publicação é substituído por um double. Assim, o baseline é rápido, determinístico
e não depende de PostgreSQL, RabbitMQ ou APIs em execução. Os testes reais de
concorrência no PostgreSQL, redelivery/inbox no MassTransit e consistência do fluxo
distribuído serão adicionados nas Etapas 3, 4 e 5, quando essas regras forem
implementadas.

## Testes de caracterização

Alguns testes têm o sufixo `AsCurrentBehavior`. A Etapa 2 inverteu o teste que
aceitava saldo omitido como zero; omissão agora retorna validação. A repetição da
mesma referência de baixa continua caracterizada até a etapa de idempotência e não
deve ser interpretada como aprovação da regra atual.

## Baseline verificado

Em 22 de agosto de 2026, a verificação integrada obteve:

| Projeto | Aprovados | Falhas | Ignorados |
| --- | ---: | ---: | ---: |
| InventoryService | 68 | 0 | 0 |
| BillingService | 71 | 0 | 0 |
| Angular | 27 | 0 | 0 |
| **Total** | **166** | **0** | **0** |

Os quatro projetos .NET compilaram em Release com zero erros e zero avisos. As
139 verificações backend também passaram dentro da imagem oficial do SDK .NET 9.
O bundle Angular de produção foi gerado sem o antigo aviso de template `NG8107`.
O contrato completo da etapa está em
[api-errors-and-validation.md](api-errors-and-validation.md).
