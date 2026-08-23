# Análise inteligente de notas fiscais

O Billing Service oferece uma análise consultiva dos dados de uma nota fiscal
usando o Google Gemini. A funcionalidade é independente da impressão: uma falha
ou ausência de configuração da IA nunca impede o fluxo principal do sistema.

## Configuração

Crie uma chave no [Google AI Studio](https://aistudio.google.com/app/apikey) e
configure-a somente como variável de ambiente. A chave não deve ser salva em
`appsettings.json`, `.env` versionado ou qualquer arquivo do repositório.

PowerShell, para a sessão atual:

```powershell
$env:GEMINI_API_KEY = "sua-chave"
```

O modelo padrão é `gemini-3.5-flash-lite`. Para trocá-lo sem alterar código:

```powershell
$env:GEMINI_MODEL = "outro-modelo-compativel"
```

Com Docker Compose, as variáveis da sessão são encaminhadas ao Billing Service:

```powershell
docker compose up --build
```

## Utilização

1. Abra a página **Notas Fiscais**.
2. Clique no ícone de estrelas da coluna **Ações**.
3. Aguarde o modal apresentar o resumo, nível de risco, pontos de conferência e
   sugestões.

O endpoint utilizado é:

```text
POST /api/invoices/{id}/ai-analysis
```

A resposta continua dentro do envelope padrão `ApiResponse<T>`. O conteúdo da
análise possui:

- `isAvailable`: informa se houve resposta válida do provedor;
- `hasAnomalies`: indica se há pontos que merecem conferência;
- `riskLevel`: `low`, `medium`, `high` ou `unavailable`;
- `summary`, `risks` e `suggestions`;
- `provider` e `analyzedAt`.

## Segurança e resiliência

- Somente número, status, total e itens da nota são enviados. Nome do cliente e
  observações não são compartilhados com o provedor.
- Código e descrição dos produtos são tratados como dados não confiáveis; o
  prompt instrui o modelo a ignorar comandos contidos nesses campos.
- A resposta é exigida em JSON Schema e novamente validada e limitada pelo
  backend.
- Cada análise é uma requisição isolada, sem histórico de conversa.
- O timeout padrão é de oito segundos.
- Chave ausente, timeout, falha HTTP ou resposta inválida produzem um resultado
  amigável com `isAvailable: false`; cadastro e impressão continuam disponíveis.

## Verificação manual rápida

Com uma nota já cadastrada, execute:

```powershell
curl.exe -X POST http://localhost:5002/api/invoices/ID_DA_NOTA/ai-analysis
```

Com a chave configurada, o retorno deve ter `isAvailable: true`. Remova
temporariamente `GEMINI_API_KEY` e repita para confirmar o fallback com
`isAvailable: false`.
