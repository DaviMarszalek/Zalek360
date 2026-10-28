using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zalek360.Application.Common;
using Zalek360.Application.Interfaces;
using Zalek360.Infrastructure.BackgroundJobs;
using Zalek360.Infrastructure.Options;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Infrastructure.Queries;
using Zalek360.Infrastructure.Repositories;
using Zalek360.Infrastructure.Services;
using Zalek360.Infrastructure.Documentos;

namespace Zalek360.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.Secao))
            .Validate(o => o.Chave.Length >= 32, "Jwt:Chave deve ter pelo menos 32 caracteres (defina JWT__CHAVE / Jwt__Chave).")
            .ValidateOnStart();
        services.Configure<ArmazenamentoOptions>(configuration.GetSection(ArmazenamentoOptions.Secao));
        services.Configure<AplicacaoOptions>(configuration.GetSection(AplicacaoOptions.Secao));
        services.Configure<BancoOptions>(configuration.GetSection(BancoOptions.Secao));
        services.Configure<RotinasOptions>(configuration.GetSection(RotinasOptions.Secao));
        services.AddOptions<EmpresaOptions>().Bind(configuration.GetSection(EmpresaOptions.Secao))
            .Validate(o => !string.IsNullOrWhiteSpace(o.NomeFantasia) && o.NomeFantasia.Length <= 200,
                "Empresa:NomeFantasia é obrigatório e deve ter até 200 caracteres.")
            .Validate(o => o.RazaoSocial.Length <= 200 && o.Endereco.Length <= 500 && (o.TextoRodape?.Length ?? 0) <= 500,
                "Empresa: razão social, endereço ou rodapé excedem o tamanho permitido.")
            .Validate(o => string.IsNullOrEmpty(o.Email) || System.Net.Mail.MailAddress.TryCreate(o.Email, out _),
                "Empresa:Email deve ser um e-mail válido.")
            .Validate(o => string.IsNullOrEmpty(o.Cnpj) || Zalek360.Domain.Common.DocumentoFiscal.CnpjValido(Zalek360.Domain.Common.DocumentoFiscal.Normalizar(o.Cnpj)),
                "Empresa:Cnpj deve ser um CNPJ válido ou ficar vazio.")
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmpresaOptions>>().Value);
        services.AddSingleton<IGeradorDocumentoOrcamento, GeradorDocumentoOrcamento>();

        var conexao = configuration.GetConnectionString("Zalek360")
                      ?? throw new InvalidOperationException("ConnectionStrings:Zalek360 não configurada.");
        // Sem EnableRetryOnFailure: a aprovação usa transação explícita (orçamento + pedido), incompatível com retry automático.
        services.AddDbContext<ZalekDbContext>(o => o.UseNpgsql(conexao));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ZalekDbContext>());
        services.AddScoped<INumeradorDocumentos, NumeradorPostgres>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IAnexoRepository, AnexoRepository>();

        services.AddScoped<IClienteQueries, ClienteQueries>();
        services.AddScoped<IOrcamentoQueries, OrcamentoQueries>();
        services.AddScoped<IPedidoQueries, PedidoQueries>();
        services.AddScoped<IDashboardQueries, DashboardQueries>();
        services.AddScoped<ICatalogoQueries, CatalogoQueries>();

        services.AddSingleton<IClock>(sp => new RelogioComFusoHorario(sp.GetRequiredService<IOptions<AplicacaoOptions>>().Value.FusoHorario));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IFileStorage, ArmazenamentoLocal>();

        services.AddHostedService<ExpiracaoOrcamentosWorker>();
        services.AddHostedService<LimpezaAnexosWorker>();
        return services;
    }
}
