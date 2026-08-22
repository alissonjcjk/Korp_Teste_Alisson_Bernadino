using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BillingService.Api.Data;

/// <summary>
/// Factory usada pelo 'dotnet ef' em design-time (ex: ao gerar migrations).
/// Instancia o BillingDbContext diretamente, sem passar pelo pipeline de DI
/// completo da aplicação. Isso evita conflitos de versão entre pacotes (ex:
/// MassTransit vs EF Core) que ocorrem quando o host tenta ser inicializado.
/// </summary>
public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();

        // Connection string usada apenas para geração de migrations.
        // Em runtime, a string é injetada via appsettings / variáveis de ambiente.
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5432;Database=billing_db;Username=korp_user;Password=korp_pass")
            .UseSnakeCaseNamingConvention();

        return new BillingDbContext(optionsBuilder.Options);
    }
}
