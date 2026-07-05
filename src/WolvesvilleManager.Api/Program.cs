using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
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

// CORS para o futuro frontend (SPA em domínio separado).
const string corsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddHostedService<ScheduledTaskRunnerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

app.Run();
