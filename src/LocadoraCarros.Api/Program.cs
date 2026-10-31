using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LocadoraCarros.Api.Erros;
using LocadoraCarros.Api.OpenApi;
using LocadoraCarros.Api.Seguranca;
using LocadoraCarros.Application;
using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Infrastructure;
using LocadoraCarros.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPoliticasDeAcesso();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// Protege login e cadastro contra força bruta: 10 tentativas por minuto por IP.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Program.LimiteAutenticacao, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

await DbInicializador.InicializarAsync(app.Services, app.Configuration);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Em produção a documentação só aparece com Documentacao:Habilitada = true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Documentacao:Habilitada"))
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(o => o.WithTitle("Locadora de Carros API")).AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program
{
    public const string LimiteAutenticacao = "autenticacao";
}
