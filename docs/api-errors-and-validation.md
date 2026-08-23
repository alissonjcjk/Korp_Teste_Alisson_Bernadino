# Contrato de erros e validações — Etapa 2

Esta etapa estabelece um contrato único para erros do Inventory Service e do
Billing Service, alinha as validações do backend e do Angular e impede que
detalhes internos sejam enviados ao usuário.

## Envelope de erro

Toda resposta de erro JSON usa os mesmos campos:

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Um ou mais erros de validação ocorreram.",
  "errors": {
    "StockBalance": [
      "O saldo inicial é obrigatório."
    ]
  },
  "traceId": "0HN...",
  "timestamp": "2026-08-22T22:00:00+00:00"
}
```

`errors` só aparece quando há erros associados a campos. `traceId` permite
correlacionar a resposta com os logs. `timestamp` é sempre produzido em UTC.
Não são retornados `detail`, stack trace, tipo CLR, SQL, caminho de arquivo nem
mensagem de exceção técnica.

## Códigos HTTP

| Status | Uso |
| --- | --- |
| 400 | JSON/model binding inválido, campo obrigatório ou regra de entrada |
| 404 | produto, nota ou rota inexistente |
| 405 | método HTTP não permitido |
| 409 | duplicidade, estoque insuficiente, estado inválido ou concorrência |
| 413 | corpo da requisição acima do limite do servidor |
| 415 | `Content-Type` não suportado |
| 503 | Inventory Service, rede ou circuit breaker indisponível |
| 500 | falha inesperada, sempre com mensagem genérica |

Rejeições 400/409 devolvidas pelo Inventory Service são preservadas pelo Billing
como erros de negócio. Falha de rede, timeout, resposta inválida e erro 5xx remoto
viram 503 com mensagem segura.

## Validações do Inventory Service

| Operação/campo | Regra |
| --- | --- |
| Criar produto — `code` | obrigatório, até 50 caracteres, sem caracteres de controle |
| Criar produto — `description` | obrigatória, até 255 caracteres |
| Criar produto — `stockBalance` | obrigatório, maior ou igual a zero, `numeric(18,4)` |
| Criar produto — `unit` | obrigatória, até 20 caracteres |
| Atualizar produto — `description` | obrigatória, até 255 caracteres |
| Atualizar produto — `unit` | obrigatória, até 20 caracteres |
| Baixar estoque — `quantity` | obrigatória, maior que zero, `numeric(18,4)` |
| Baixar estoque — `invoiceReference` | obrigatória, até 100 caracteres, sem caracteres de controle |

Campos numéricos são nullable no DTO de entrada apenas para distinguir “omitido”
de zero. O Swagger os marca como obrigatórios e não nullable.

## Validações do Billing Service

| Operação/campo | Regra |
| --- | --- |
| Criar nota — `customerName` | opcional, até 255 caracteres |
| Criar nota — `notes` | opcional, até 1000 caracteres |
| Criar nota — `items` | obrigatório, de 1 a 100 itens; nenhum item pode ser nulo |
| Item — `productId` | GUID não vazio |
| Item — `quantity` | obrigatória, maior que zero, `numeric(18,4)` |
| Item — `unitPrice` | obrigatório, maior ou igual a zero, `numeric(18,4)` |
| Imprimir — `Idempotency-Key` | obrigatório, não vazio, até 100 caracteres |

O total de cada item é arredondado para quatro casas com
`MidpointRounding.AwayFromZero`. O total da nota soma esses valores já
arredondados. Tanto a linha quanto o agregado precisam caber em
`numeric(18,4)`; caso contrário, a API responde 400 antes de acessar o banco.

## Comportamento do Angular

- O interceptor lê `message`, `errors` e `traceId` do envelope.
- Em 400, mensagens por campo têm precedência no toast.
- 404, 409, 503 e 500 têm títulos e fallbacks próprios.
- Campos legados como `detail` e stack trace são ignorados.
- Saldo, quantidade e preço aplicam 14 dígitos inteiros e quatro casas decimais.
- Totais de linha/agregado e o limite de 100 itens são validados antes do envio.
- Na edição de produto, somente `description` e `unit` são enviados; código e
  saldo ficam realmente desabilitados.

## Como verificar

Verificação local completa:

```powershell
pwsh -File .\scripts\verify-stage2.ps1 -SkipRestore
```

Para repetir os testes backend no runtime oficial .NET 9 usado pelos Dockerfiles:

```powershell
pwsh -File .\scripts\verify-stage2.ps1 -SkipRestore -VerifyDotNet9Container
```

Com os serviços em execução, alguns testes manuais úteis são:

```powershell
curl.exe -i -X POST http://localhost:5001/api/products `
  -H "Content-Type: application/json" -d "{}"

curl.exe -i -X POST http://localhost:5001/api/products `
  -H "Content-Type: text/plain" -d "{}"

curl.exe -i http://localhost:5002/api/rota-inexistente
```

Os três devem retornar o envelope padronizado, respectivamente com 400, 415 e
404.

## Limites deliberadamente pendentes

Esta etapa não altera o fluxo distribuído de impressão/baixa. Concorrência de
impressão, Inbox/Outbox no endpoint consumidor, redelivery e idempotência da
baixa por referência continuam destinados às Etapas 3 e 4. Os testes que
caracterizam esses comportamentos permanecem visíveis para evitar que sejam
confundidos com garantias já entregues.
