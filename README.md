<div align="center">

# Korp ERP

### Estoque e emissão de notas fiscais em uma arquitetura de microsserviços

Angular 19 · ASP.NET Core 9 · PostgreSQL · RabbitMQ · Google Gemini

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3-FF6600?logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)

</div>

## Sobre o projeto

O **Korp ERP** é uma aplicação full stack para gerenciamento de produtos,
controle de estoque e emissão de notas fiscais. A solução separa os contextos de
estoque e faturamento, utiliza eventos para a baixa assíncrona e oferece uma
análise consultiva opcional de notas fiscais com Inteligência Artificial.

O projeto foi desenvolvido como desafio técnico, priorizando código legível,
contratos HTTP consistentes, validações nas duas camadas, resiliência e uma
interface moderna e responsiva.

## Funcionalidades

- cadastro, edição, pesquisa e exclusão de produtos;
- controle de saldo com precisão de quatro casas decimais;
- criação de notas com múltiplos itens e autocomplete de produtos;
- cálculo e validação dos totais no frontend e no backend;
- impressão/fechamento da nota com chave de idempotência;
- baixa assíncrona de estoque por RabbitMQ;
- tratamento padronizado de validações e erros;
- logs estruturados, health checks e Swagger;
- análise consultiva de notas com Google Gemini;
- fallback seguro: indisponibilidade da IA não bloqueia o sistema.

## Capturas de tela

> Os espaços abaixo estão preparados para receber as imagens da aplicação em
> execução. As instruções para adicioná-las estão em
> [`docs/images/README.md`](docs/images/README.md).

### Produtos

> 🖼️ **Espaço reservado:** listagem e gerenciamento de produtos.

<!-- Quando a imagem existir, remova este comentário e use:
![Tela de produtos do Korp ERP](docs/images/produtos.png)
-->

### Cadastro de produto

> 🖼️ **Espaço reservado:** modal de criação ou edição de produto.

<!-- Quando a imagem existir, remova este comentário e use:
![Cadastro de produto no Korp ERP](docs/images/cadastro-produto.png)
-->

### Notas fiscais

> 🖼️ **Espaço reservado:** listagem e emissão de notas fiscais.

<!-- Quando a imagem existir, remova este comentário e use:
![Tela de notas fiscais do Korp ERP](docs/images/notas-fiscais.png)
-->

### Análise com Inteligência Artificial

> 🖼️ **Espaço reservado:** modal com resumo, riscos e sugestões da IA.

<!-- Quando a imagem existir, remova este comentário e use:
![Análise de nota fiscal com IA](docs/images/analise-ia.png)
-->

## Arquitetura

```mermaid
flowchart LR
    U[Usuário] --> WEB[Angular 19]
    WEB -->|Produtos| INV[Inventory Service]
    WEB -->|Notas| BILL[Billing Service]
    BILL -->|Consulta HTTP| INV
    BILL -->|Evento de impressão| MQ[(RabbitMQ)]
    MQ -->|Baixa de estoque| INV
    INV --> IDB[(inventory_db)]
    BILL --> BDB[(billing_db)]
    BILL -.->|Análise opcional| GEMINI[Google Gemini]
```

| Camada | Tecnologias |
| --- | --- |
| Frontend | Angular 19, RxJS, Reactive Forms, Signals e Tailwind CSS |
| APIs | ASP.NET Core 9, FluentValidation, Serilog e Swagger |
| Persistência | Entity Framework Core 9, Npgsql e PostgreSQL 16 |
| Mensageria | RabbitMQ e MassTransit |
| Resiliência | Polly, health checks, timeout e fallback |
| IA | Gemini API com resposta estruturada por JSON Schema |

Mais detalhes estão em [`detalhamento_tecnico.md`](detalhamento_tecnico.md).

## Como executar

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/);
- [Node.js 20 LTS](https://nodejs.org/) e npm;
- opcionalmente, [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
  para executar ou testar os serviços fora dos contêineres.

### 1. Clone o repositório

```bash
git clone https://github.com/alissonjcjk/Korp_Teste_Alisson_Bernadino.git
cd Korp_Teste_Alisson_Bernadino
```

### 2. Suba a infraestrutura e os serviços

```bash
docker compose up --build -d
```

O Compose inicia PostgreSQL, RabbitMQ, Inventory Service e Billing Service. As
migrations são aplicadas automaticamente na inicialização.

### 3. Inicie o frontend

```bash
cd frontend/korp-erp-frontend
npm ci
npm start
```

Acesse **http://localhost:4200**.

### 4. Configure a IA — opcional

A aplicação funciona normalmente sem IA. Para habilitar a análise, defina a
chave apenas como variável de ambiente **antes** de subir o Billing Service.

No PowerShell:

```powershell
$env:GEMINI_API_KEY = "sua-chave-do-google-ai-studio"
docker compose up --build -d billing-service
```

Não salve nem publique a chave no repositório. Consulte a
[`documentação da integração`](docs/ai-invoice-analysis.md) para detalhes.

## Endereços locais

| Recurso | Endereço |
| --- | --- |
| Aplicação Angular | http://localhost:4200 |
| Inventory API + Swagger | http://localhost:5001 |
| Billing API + Swagger | http://localhost:5002 |
| RabbitMQ Management | http://localhost:15672 |
| Health — Inventory | http://localhost:5001/health |
| Health — Billing | http://localhost:5002/health |

O acesso local padrão ao RabbitMQ Management é `guest` / `guest`.

## Endpoints principais

### Produtos

| Método | Endpoint | Descrição |
| --- | --- | --- |
| `GET` | `/api/products?search=` | Lista e pesquisa produtos |
| `GET` | `/api/products/{id}` | Consulta um produto |
| `POST` | `/api/products` | Cadastra um produto |
| `PUT` | `/api/products/{id}` | Atualiza descrição e unidade |
| `DELETE` | `/api/products/{id}` | Exclui um produto |
| `POST` | `/api/products/{id}/deduct-stock` | Realiza baixa de estoque |

### Notas fiscais

| Método | Endpoint | Descrição |
| --- | --- | --- |
| `GET` | `/api/invoices` | Lista notas fiscais |
| `GET` | `/api/invoices/{id}` | Consulta uma nota |
| `POST` | `/api/invoices` | Cria uma nota |
| `POST` | `/api/invoices/{id}/print` | Imprime e fecha a nota |
| `POST` | `/api/invoices/{id}/ai-analysis` | Solicita análise consultiva por IA |

## Testes

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

Os scripts e o baseline de qualidade estão documentados em
[`docs/testing.md`](docs/testing.md).

## Estrutura do repositório

```text
.
├── docs/                         # Guias técnicos e imagens
├── frontend/korp-erp-frontend/  # Aplicação Angular
├── infra/postgres/              # Inicialização dos bancos
├── services/
│   ├── billing-service/         # Notas fiscais e integração de IA
│   └── inventory-service/       # Produtos e estoque
├── docker-compose.yml
├── Korp.Erp.slnx
└── detalhamento_tecnico.md
```

## Documentação

- [Detalhamento técnico](detalhamento_tecnico.md)
- [Erros e validações](docs/api-errors-and-validation.md)
- [Análise de notas com IA](docs/ai-invoice-analysis.md)
- [Testes e baseline](docs/testing.md)

## Observações de segurança

- nunca versione chaves, tokens ou arquivos `.env` com credenciais;
- os usuários e senhas do Compose são apenas para desenvolvimento local;
- a análise de IA é consultiva e não substitui validação humana;
- nome do cliente e observações da nota não são enviados ao Gemini.

## Próximas evoluções

- reforçar atomicidade em impressões simultâneas;
- ativar Inbox e redelivery controlado no consumidor;
- adicionar recuperação/compensação para falhas na baixa assíncrona;
- ampliar os testes de integração com PostgreSQL e RabbitMQ;
- adicionar autenticação e autorização.

---

<div align="center">

Desenvolvido para o desafio técnico da **Korp**.

</div>
