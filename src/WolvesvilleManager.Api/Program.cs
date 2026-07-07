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

builder.Services.AddHostedService<ScheduledTaskRunnerService>();

var app = builder.Build();

// Aplica migrações pendentes no startup — sem acesso fácil ao banco no plano gratuito do Azure,
// este é o jeito mais simples de manter o esquema em dia a cada deploy.
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<WolvesvilleManager.Infrastructure.Persistence.AppDbContext>()
        .Database.Migrate();

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
