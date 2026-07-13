using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Exceptions;

namespace WolvesvilleManager.Api.Filters;

/// <summary>
/// Tratamento central de exceções: converte erros conhecidos em respostas HTTP
/// consistentes (ProblemDetails), evitando try/catch repetido em cada action.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var (status, title) = context.Exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Não encontrado"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            ApiKeyUnprotectException => (StatusCodes.Status409Conflict, "Chave de API ilegível"),
            WolvesvilleApiException { IsRateLimit: true } =>
                (StatusCodes.Status429TooManyRequests, "Limite de requisições da API do Wolvesville"),
            WolvesvilleApiException => (StatusCodes.Status502BadGateway, "Erro na API do Wolvesville"),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Falha de comunicação com a API do Wolvesville"),
            // A API do Wolvesville é fracamente tipada; um formato inesperado não deve virar 500 opaco.
            JsonException => (StatusCodes.Status502BadGateway, "Resposta em formato inesperado da API do Wolvesville"),
            _ => (0, string.Empty),
        };

        if (status == 0) return; // exceção não mapeada — deixa o pipeline padrão tratar (500)

        if (status >= 500)
            _logger.LogWarning(context.Exception, "Erro tratado pelo ApiExceptionFilter");

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = context.Exception.Message,
        })
        { StatusCode = status };

        context.ExceptionHandled = true;
    }
}
