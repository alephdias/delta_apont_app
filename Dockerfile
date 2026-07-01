# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaura dependencias (camada cacheavel) usando so o csproj
COPY backend/DeltaApp.Api/DeltaApp.Api.csproj backend/DeltaApp.Api/
RUN dotnet restore backend/DeltaApp.Api/DeltaApp.Api.csproj

# Copia o restante do backend e publica
COPY backend/ backend/
RUN dotnet publish backend/DeltaApp.Api/DeltaApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# O Render define PORT em runtime; Program.cs escuta nela.
ENTRYPOINT ["dotnet", "DeltaApp.Api.dll"]
