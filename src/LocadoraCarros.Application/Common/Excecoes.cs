namespace LocadoraCarros.Application.Common;

public abstract class AppException(string message) : Exception(message);

/// <summary>404 - recurso inexistente ou fora do alcance do usuário.</summary>
public sealed class RecursoNaoEncontradoException(string recurso) : AppException($"{recurso} não encontrado(a).");

/// <summary>403 - usuário autenticado sem permissão para a operação.</summary>
public sealed class AcessoNegadoException(string message) : AppException(message);

/// <summary>409 - conflito com o estado atual (duplicidade, período ocupado...).</summary>
public sealed class ConflitoException(string message) : AppException(message);

/// <summary>400 - dados de entrada inválidos.</summary>
public sealed class ValidacaoException(string message) : AppException(message);

/// <summary>401 - falha de autenticação.</summary>
public sealed class CredenciaisInvalidasException(string message = "Login ou senha inválidos.") : AppException(message);
