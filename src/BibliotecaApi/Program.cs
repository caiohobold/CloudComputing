using BibliotecaApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Em produção (EC2) a connection string vem da variável de ambiente ConnectionStrings__Default
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplica as migrations pendentes ao subir (cria a tabela no RDS automaticamente)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger habilitado também em produção: é a "tela" usada na apresentação
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Health check: mostra se a API está no ar e se consegue falar com o banco
app.MapGet("/health", async (AppDbContext db) =>
{
    var bancoOk = await db.Database.CanConnectAsync();
    var resultado = new
    {
        status = bancoOk ? "ok" : "erro",
        banco = bancoOk ? "conectado" : "indisponível",
        servidor = Environment.MachineName,
        horario = DateTime.UtcNow
    };
    return bancoOk ? Results.Ok(resultado) : Results.Json(resultado, statusCode: 503);
}).WithTags("Health");

app.Run();
