-- Script de inicialização do PostgreSQL
-- Cria os bancos de dados para o Inventory e Billing Service

-- Banco do Serviço de Estoque
SELECT 'CREATE DATABASE inventory_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'inventory_db')
\gexec

-- Banco do Serviço de Faturamento
SELECT 'CREATE DATABASE billing_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'billing_db')
\gexec

-- Conceder privilégios ao usuário
GRANT ALL PRIVILEGES ON DATABASE inventory_db TO korp_user;
GRANT ALL PRIVILEGES ON DATABASE billing_db TO korp_user;
