# Detalhamento Técnico - Sistema de Emissão de Notas Fiscais

Este documento apresenta o detalhamento técnico da solução desenvolvida, conforme as especificações solicitadas no teste.

## 1. Quais ciclos de vida do Angular foram utilizados?
Foram utilizados os seguintes *Lifecycle Hooks* no frontend:
- **`ngOnInit`**: Utilizado amplamente (`ProductsPageComponent`, `InvoicesPageComponent` e `InvoiceFormModalComponent`) para disparar as requisições iniciais às APIs, carregar a listagem de dados e popular os formulários reativos assim que os componentes são renderizados na tela.

## 2. Uso da biblioteca RxJS
Sim, a biblioteca **RxJS** foi utilizada de forma extensiva e reativa no projeto:
- **Transformação de Dados (`map`)**: Utilizado nos *Services* para desembrulhar o contrato padrão do backend (`ApiResponse<T>`) e repassar apenas os dados puros para os componentes visuais.
- **Busca em Tempo Real (Autocomplete/Typeahead)**: Na tela de inclusão de itens da Nota Fiscal, utilizamos um `Subject` atrelado aos operadores `debounceTime(300)` (para não enviar requisições a cada tecla digitada), `distinctUntilChanged` (evita requisições se a busca não mudou) e `switchMap` (cancela requisições anteriores caso o usuário digite rápido demais). Isso garante uma pesquisa de produtos no banco extremamente performática.

## 3. Quais outras bibliotecas foram utilizadas e para qual finalidade?
No frontend, além do núcleo do Angular:
- **Tailwind CSS (v3)**: Utilizado para construção do design system e estilização utility-first, dispensando a escrita de arquivos CSS gigantes e permitindo a rápida criação da interface baseada em *Glassmorphism*.

## 4. Para componentes visuais, quais bibliotecas foram utilizadas?
**Nenhuma biblioteca externa de componentes visuais** (como Angular Material, Bootstrap ou PrimeNG) foi utilizada. 
Optou-se por construir todos os componentes do zero (Tabelas, Modais, Dropdowns, Badges, Botões) focando em entregar um design exclusivo, altamente polido, moderno (premium) e responsivo, demonstrando total domínio sobre HTML e CSS (via Tailwind).

## 5. Gerenciamento de dependências no Golang
*Não aplicável.* O projeto foi desenvolvido integralmente utilizando o ecossistema C# (.NET).

## 6. Quais frameworks foram utilizados no Golang ou C#?
No ecossistema **C#**, foram adotados os seguintes frameworks e bibliotecas nos microsserviços (Inventory e Billing):
- **ASP.NET Core 9.0**: Framework base para a construção das Web APIs.
- **Entity Framework Core (EF Core 9)**: ORM utilizado para a persistência e mapeamento objeto-relacional com o banco de dados PostgreSQL.
- **Polly**: Biblioteca de resiliência. Utilizada no `BillingService` para criar políticas de *Retry* e *Circuit Breaker* ao se comunicar via HTTP com o `InventoryService`, garantindo robustez a falhas de rede.
- **Serilog**: Utilizado para captura e formatação centralizada de logs estruturados da aplicação (console/file).
- **Npgsql**: Provider oficial de banco de dados do PostgreSQL para .NET e EF Core.

## 7. Como foram tratados os erros e exceções no backend?
Foi construída uma arquitetura baseada em **Exceções de Domínio Customizadas** (Domain Exceptions) unida a um **Middleware de Interceptação Global** (`GlobalExceptionHandlerMiddleware`):
- O fluxo de negócio lança exceções especializadas, como `ProductNotFoundException`, `InsufficientStockException`, `DuplicateIdempotencyKeyException`, ou `ConcurrencyConflictException`.
- O Middleware global (que envelopa todo o request) captura qualquer falha e as mapeia para o Status HTTP adequado (ex: `404 Not Found`, `400 Bad Request`, `409 Conflict`).
- O retorno é *sempre* padronizado no envelope `ApiResponse<T>`, garantindo que o frontend consiga tratar o feedback adequadamente (ex: "Estoque insuficiente") em vez de quebrar a aplicação.

## 8. Uso de LINQ e de que forma
Sim, o **LINQ** foi utilizado de ponta a ponta na camada de Serviços do backend. Formas de uso:
- **Projeções Diretas (`.Select()`)**: Usado para extrair dados diretamente para os DTOs (`ProductResponse`, `StockBalanceResponse`), evitando os problemas de *over-fetching* (trazer dados excessivos do banco) inerentes ao uso puro do EF Core.
- **Filtros Dinâmicos (`.Where()`)**: Na busca de produtos (ProductsController), o `Where` condicional é injetado na árvore de expressões (`IQueryable`) para buscar termos no código ou na descrição.
- **Lógica e Agregação (`.AnyAsync()`, `.MaxAsync()`)**: O `.AnyAsync` foi usado para validação rápida de chaves únicas (ex: códigos de produto já cadastrados). O `.MaxAsync` foi utilizado na geração inteligente do número sequencial (auto-incremento manual) das Notas Fiscais no momento da criação.

---

### Bônus: Requisitos Opcionais Implementados 🚀

- **Tratamento de Concorrência (OCC)**: O abatimento de estoque no `InventoryService` utiliza o conceito de concorrência otimista (Optimistic Concurrency Control) valendo-se do campo nativo `xmin` do PostgreSQL. Se duas notas tentarem abater o mesmo produto no exato mesmo milissegundo, a transação da segunda nota irá detectar conflito de concorrência e não deixará o estoque ficar negativo ou corrompido, disparando um erro apropriado (Status 409).
- **Implementação de Idempotência**: O endpoint de "Impressão de Nota Fiscal" (o que aciona a baixa do estoque) requer o envio do cabeçalho `Idempotency-Key` pelo frontend. Essa chave é atrelada à NF no banco. Se o request sofrer *timeout*, falha de rede intermitente e tentar ser disparado novamente (*retry* do Polly), a idempotência reconhecerá a chave e retornará sucesso imediato sem deduzir o estoque duas vezes.
