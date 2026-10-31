using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LocadoraCarros.Api.OpenApi;

/// <summary>Documenta o esquema Bearer (JWT) no OpenAPI para o botão "Authorize" da UI.</summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info.Title = "Locadora de Carros API";
        document.Info.Description = "Faça login em POST /api/auth/login e envie o token no header: Authorization: Bearer {token}.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Token JWT obtido em /api/auth/login"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Esquema, document)] = []
        });
        return Task.CompletedTask;
    }
}
