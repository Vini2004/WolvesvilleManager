namespace WolvesvilleManager.Api.Security;

/// <summary>
/// Autenticação mínima da aplicação: exige o header X-Api-Key com o valor configurado
/// em "Security:AppApiKey". Sem isso, qualquer pessoa com a URL gerenciaria todos os
/// clãs registrados. Se a configuração estiver vazia (dev local), a checagem é desativada
/// com um aviso no log.
/// </summary>
public class ApiKeyAuthMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly string? _expectedKey;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _expectedKey = configuration["Security:AppApiKey"];
        if (string.IsNullOrWhiteSpace(_expectedKey))
            logger.LogWarning(
                "Security:AppApiKey não configurada — a API está SEM autenticação. " +
                "Configure antes de expor fora do localhost.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // O formulário público de votação é aberto por design: quem tem o link vota.
        // O token aleatório na rota é a credencial; o resto da API segue exigindo a chave.
        var isPublicPoll = context.Request.Path.StartsWithSegments("/api/poll");

        if (!isPublicPoll && !string.IsNullOrWhiteSpace(_expectedKey))
        {
            var provided = context.Request.Headers[HeaderName].FirstOrDefault();
            if (provided != _expectedKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = $"Header {HeaderName} ausente ou inválido." });
                return;
            }
        }

        await _next(context);
    }
}
