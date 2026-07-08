using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Api.Background;
using WolvesvilleManager.Api.Filters;
using WolvesvilleManager.Api.Security;
using WolvesvilleManager.Application;
using WolvesvilleManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Keyring persistido em disco + nome fixo: sem isso, um redeploy invalidaria
// todas as chaves de API criptografadas no banco.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(keysPath))
    keysPath = Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("WolvesvilleManager");

builder.Services
    .AddControllers(options => options.Filters.Add<ApiExceptionFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// CORS para o frontend (SPA em domínio separado). Em desenvolvimento aceita qualquer
// origem localhost — o Vite troca de porta quando a padrão (5173) está ocupada.
const string corsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
{
    policy.AllowAnyHeader().AllowAnyMethod();
    if (builder.Environment.IsDevelopment())
        policy.SetIsOriginAllowed(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback);
    else
        policy.WithOrigins(allowedOrigins);
}));

// Loop in-process do agendador. Em produção sem Always On (App Service F1) ele manteria o app
// CPU-ativo 24/7 e estouraria a cota de 60 min de CPU/dia — lá o gatilho é o cron externo
// (POST /api/scheduler/run, ex.: cron-job.org). Por padrão só liga em Development; para forçar
// em outro ambiente, defina Scheduler:RunBackgroundLoop=true.
if (builder.Configuration.GetValue("Scheduler:RunBackgroundLoop", builder.Environment.IsDevelopment()))
    builder.Services.AddHostedService<ScheduledTaskRunnerService>();

var app = builder.Build();

// Aplica migrações pendentes no startup — sem acesso fácil ao banco no plano gratuito do Azure,
// este é o jeito mais simples de manter o esquema em dia a cada deploy.
// Não pode ser fatal: se o Azure SQL free estiver indisponível (auto-pausa / cota mensal de
// vCore-segundos esgotada), deixar a exceção subir derruba o processo (HTTP 500.30 "app failed
// to start") e nem os cabeçalhos CORS saem — o frontend só vê "não foi possível conectar".
// Logando e seguindo, o app sobe e devolve erros JSON legíveis; basta reiniciar quando o banco voltar.
try
{
    using var scope = app.Services.CreateScope();
    // Teto de tempo: com o banco pausado/indisponível, o retry do EF poderia travar minutos e
    // estourar o limite de inicialização do Azure. Cancelando em 25s, o app sobe rápido mesmo assim.
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    await scope.ServiceProvider.GetRequiredService<WolvesvilleManager.Infrastructure.Persistence.AppDbContext>()
        .Database.MigrateAsync(cts.Token);
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogError(ex, "Falha ao aplicar migrações no startup — o app vai subir mesmo assim. "
            + "Provável banco indisponível (Azure SQL free pausado ou cota mensal esgotada).");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Em desenvolvimento o frontend fala direto com a porta HTTP (VITE_API_URL) — redirecionar para
// HTTPS exigiria que o certificado dev do ASP.NET Core estivesse confiável no SO, o que trava o
// fetch do navegador silenciosamente (aparece como "não foi possível conectar").
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

app.Run();
