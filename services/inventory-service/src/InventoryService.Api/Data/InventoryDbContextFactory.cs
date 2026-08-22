using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryService.Api.Data;

/// <summary>
/// Factory usada pelo 'dotnet ef' em design-time (ex: ao gerar migrations).
/// Instancia o InventoryDbContext diretamente, sem passar pelo pipeline de DI
/// completo da aplicação. Isso evita conflitos de versão entre pacotes (ex:
/// MassTransit vs EF Core) que ocorrem quando o host tenta ser inicializado.
/// </summary>
public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();

        // Connection string usada apenas para geração de migrations.
        // Em runtime, a string é injetada via appsettings / variáveis de ambiente.
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5432;Database=inventory_db;Username=korp_user;Password=korp_pass")
            .UseSnakeCaseNamingConvention();

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
