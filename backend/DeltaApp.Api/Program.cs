using System.Text;
using DeltaApp.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Render injeta a porta via variavel de ambiente PORT. Em producao escutamos nela (0.0.0.0).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// --- Services ---
builder.Services.AddControllers();

// EF Core + PostgreSQL (Supabase).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Autenticacao: valida o JWT emitido pelo Supabase Auth ---
var supabaseUrl = builder.Configuration["Supabase:Url"]?.TrimEnd('/');
var jwtSecret = builder.Configuration["Supabase:JwtSecret"];
var issuer = string.IsNullOrEmpty(supabaseUrl) ? null : $"{supabaseUrl}/auth/v1";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;

        var tvp = new TokenValidationParameters
        {
            ValidateIssuer = issuer is not null,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };

        if (!string.IsNullOrEmpty(jwtSecret))
        {
            // Modo legado HS256: JWT secret compartilhado do Supabase (Settings -> API -> JWT Secret).
            tvp.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        }
        else if (issuer is not null)
        {
            // Modo assimetrico (chaves de assinatura novas): busca as chaves publicas via discovery.
            options.Authority = issuer;
        }

        options.TokenValidationParameters = tvp;
    });

builder.Services.AddAuthorization();

// Swagger / OpenAPI com suporte a Bearer para testar endpoints autenticados.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole o access_token do Supabase (sem o prefixo 'Bearer ')."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
