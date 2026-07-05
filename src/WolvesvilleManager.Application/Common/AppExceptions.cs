namespace WolvesvilleManager.Application.Common;

/// <summary>Recurso não encontrado — vira HTTP 404 na camada de API.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Violação de regra de negócio ou entrada inválida — vira HTTP 400 na camada de API.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
