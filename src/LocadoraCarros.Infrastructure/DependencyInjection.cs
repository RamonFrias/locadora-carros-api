using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Infrastructure.Persistence;
using LocadoraCarros.Infrastructure.Persistence.Repositories;
using LocadoraCarros.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LocadoraCarros.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provedor = configuration["Database:Provider"] ?? "Sqlite";
        var conexao = configuration.GetConnectionString("Locadora")
            ?? throw new InvalidOperationException("Connection string 'Locadora' não configurada.");

        services.AddDbContext<AppDbContext>(options => _ = provedor switch
        {
            "Sqlite" => options.UseSqlite(conexao),
            // Retentativas cobrem o "acordar" do Azure SQL serverless depois de pausado.
            "SqlServer" => options.UseSqlServer(conexao, sql => sql.EnableRetryOnFailure(5)),
            "MySql" => options.UseMySQL(conexao),
            _ => throw new InvalidOperationException($"Database:Provider '{provedor}' não suportado. Use MySql, Sqlite ou SqlServer.")
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILojaRepository, LojaRepository>();
        services.AddScoped<ICarroRepository, CarroRepository>();
        services.AddScoped<ILocacaoRepository, LocacaoRepository>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IRelogio, RelogioBrasilia>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddJwtBearer(configuration);
        return services;
    }

    private static void AddJwtBearer(this IServiceCollection services, IConfiguration configuration)
    {
        var secao = configuration.GetSection(JwtOptions.Secao);
        services.Configure<JwtOptions>(secao);
        var jwt = secao.Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.ObterChave(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtOptions.ClaimNome,
                    RoleClaimType = JwtOptions.ClaimPerfil
                };
            });
    }
}
