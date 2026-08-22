# Testes e baseline reproduzível

Este documento descreve o portão de qualidade criado na Etapa 1. A suíte registra
o comportamento atual e protege os fluxos que já funcionam; ela não significa que
os problemas levantados na análise inicial estejam corrigidos.

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

Alguns testes têm o sufixo `AsCurrentBehavior`. Eles documentam defeitos já
confirmados, como saldo omitido aceito como zero e repetição da mesma referência
de baixa. Esses testes devem ser invertidos para expressar o comportamento correto
na etapa responsável pela correção; não devem ser interpretados como aprovação da
regra atual.

## Baseline verificado

Em 22 de agosto de 2026, a verificação integrada obteve:

| Projeto | Aprovados | Falhas | Ignorados |
| --- | ---: | ---: | ---: |
| InventoryService | 28 | 0 | 0 |
| BillingService | 24 | 0 | 0 |
| Angular | 11 | 0 | 0 |
| **Total** | **63** | **0** | **0** |

Os quatro projetos .NET compilaram em Release com zero erros e zero avisos. O
bundle Angular de produção também foi gerado; permanece apenas o aviso de template
`NG8107` que já existia antes desta etapa e não impede a compilação.
