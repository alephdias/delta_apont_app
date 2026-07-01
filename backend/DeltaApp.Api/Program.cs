using DeltaApp.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Render injeta a porta via variavel de ambiente PORT. Em producao escutamos nela (0.0.0.0).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// --- Services ---
builder.Services.AddControllers();

// EF Core + PostgreSQL (Supabase). Connection string vem de appsettings/variaveis de ambiente.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: origens vem de config (Cors:AllowedOrigins). Em dev, cai no fallback do Vite.
const string CorsPolicy = "DeltaAppCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "http://localhost:5173", // Vite dev
        "http://localhost:4173"  // Vite preview
    };
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Aplica migracoes pendentes no startup (cria/atualiza o schema no deploy).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- HTTP pipeline ---
app.UseSwagger();
app.UseSwaggerUI();

// Render/Vercel terminam o TLS no proxy; so redirecionamos para HTTPS em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
