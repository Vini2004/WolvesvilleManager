using System.Net;

namespace WolvesvilleManager.Domain.Exceptions;

/// <summary>Erro retornado pela API do Wolvesville (status não-2xx).</summary>
public class WolvesvilleApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public WolvesvilleApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public bool IsRateLimit => StatusCode == HttpStatusCode.TooManyRequests;
}
