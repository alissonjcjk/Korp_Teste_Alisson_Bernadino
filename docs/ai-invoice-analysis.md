# Análise inteligente de notas fiscais

O Billing Service oferece uma análise consultiva dos dados de uma nota fiscal
usando a Groq API. A funcionalidade é independente da impressão: uma falha
ou ausência de configuração da IA nunca impede o fluxo principal do sistema.

## Configuração

Crie uma chave no [GroqCloud Console](https://console.groq.com/keys). A chave não
deve ser salva em `appsettings.json`, no código ou em qualquer arquivo
versionado. Para Docker Compose, use o `.env` local, já protegido pelo
`.gitignore`:

```powershell
Copy-Item .env.example .env
# Abra .env e substitua o valor de GROQ_API_KEY.
docker compose up --build -d billing-service
```

Como alternativa, configure apenas a sessão atual do PowerShell:

```powershell
$env:GROQ_API_KEY = "sua-chave"
```

O modelo padrão é `openai/gpt-oss-20b`. Para trocá-lo sem alterar código:

```powershell
$env:GROQ_MODEL = "outro-modelo-compativel"
```

As variáveis da sessão também são encaminhadas pelo Docker Compose:

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
- As anomalias seguem critérios objetivos definidos no prompt; dados ausentes,
  preço de mercado, impostos e outras informações não enviadas não podem ser
  inventados como risco.
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

Com a chave configurada, o retorno deve ter `isAvailable: true`. Para validar o
fallback, use temporariamente um ambiente sem `GROQ_API_KEY` e recrie o Billing
Service; o resultado deverá ter `isAvailable: false` sem impedir a impressão.
