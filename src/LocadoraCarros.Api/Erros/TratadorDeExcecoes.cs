using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocadoraCarros.Api.Erros;

/// <summary>Converte exceções em respostas Problem Details (RFC 9457) com status HTTP coerente.</summary>
internal sealed class TratadorDeExcecoes(IProblemDetailsService problemDetails, ILogger<TratadorDeExcecoes> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, titulo) = exception switch
        {
            ValidacaoException => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            CredenciaisInvalidasException => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            AcessoNegadoException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflitoException or DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflito ao gravar os dados"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);

        var detalhe = exception switch
        {
            AppException or DomainException => exception.Message,
            DbUpdateException => "Os dados foram alterados por outra operação ou violam uma restrição de unicidade. Tente novamente.",
            _ => "Ocorreu um erro inesperado. Tente novamente mais tarde."
        };

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo, Detail = detalhe }
        });
    }
}
