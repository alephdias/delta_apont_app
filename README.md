# delta-app

Plataforma com **aplicação web** (React) + **software desktop** (WPF) consumindo uma **API REST em C# / .NET 9**, com banco **PostgreSQL no Supabase**.

## Arquitetura

```
delta-app/
├── backend/   DeltaApp.Api   -> ASP.NET Core 9 Web API (EF Core + Npgsql)  -> Render
├── web/       React + Vite + TypeScript                                    -> Vercel
├── desktop/   DeltaApp.Desktop -> WPF (.NET 9), consome a mesma API
├── docs/      Documentação do projeto
└── delta-app.sln
```

| Camada   | Stack                                   | Hospedagem |
|----------|-----------------------------------------|------------|
| Frontend | React 19 + Vite + TypeScript            | Vercel     |
| Backend  | ASP.NET Core 9 (controllers) + EF Core  | Render     |
| Banco    | PostgreSQL                              | Supabase   |
| Desktop  | WPF (.NET 9)                            | Local/MSI  |

## Pré-requisitos

- .NET SDK 9
- Node.js 24+
- Conta no Supabase (string de conexão PostgreSQL)

## Configuração

### Backend (`backend/DeltaApp.Api`)
1. Defina a string de conexão do Supabase em `appsettings.Development.json` (não versionado) ou via variável de ambiente:
   ```
   ConnectionStrings__DefaultConnection="Host=...pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.<ref>;Password=<senha>;SSL Mode=Require;Trust Server Certificate=true"
   ```
2. Confie no certificado de desenvolvimento HTTPS (uma vez):
   ```
   dotnet dev-certs https --trust
   ```
3. Crie/aplique o schema no banco:
   ```
   dotnet ef database update --project backend/DeltaApp.Api
   ```
4. Rode a API:
   ```
   dotnet run --project backend/DeltaApp.Api
   ```
   - API: https://localhost:7291
   - Swagger: https://localhost:7291/swagger

### Frontend (`web`)
1. Ajuste `web/.env` (`VITE_API_URL`) se necessário.
2. Instale e rode:
   ```
   cd web
   npm install
   npm run dev
   ```
   - App: http://localhost:5173

### Desktop (`desktop/DeltaApp.Desktop`)
```
dotnet run --project desktop/DeltaApp.Desktop
```

## Endpoints de exemplo
`GET/POST/PUT/DELETE /api/items`
