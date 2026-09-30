# Zalek360 API — ASP.NET Core 8 (Clean Architecture)
# Contexto de build: pasta backend/  →  docker build -t zalek360-api ./backend

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Restore em camada própria (cache): só os .csproj necessários para a API.
COPY src/Zalek360.Domain/Zalek360.Domain.csproj src/Zalek360.Domain/
COPY src/Zalek360.Application/Zalek360.Application.csproj src/Zalek360.Application/
COPY src/Zalek360.Infrastructure/Zalek360.Infrastructure.csproj src/Zalek360.Infrastructure/
COPY src/Zalek360.Api/Zalek360.Api.csproj src/Zalek360.Api/
RUN dotnet restore src/Zalek360.Api/Zalek360.Api.csproj
COPY src/ src/
RUN dotnet publish src/Zalek360.Api/Zalek360.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# A imagem Debian 12 já inclui ICU (formatação pt-BR) e tzdata (fuso America/Sao_Paulo).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    Armazenamento__CaminhoLocal=/app/data/uploads
COPY --from=build /app/publish .
# Pasta de anexos (montada como volume no docker-compose) com dono = usuário não-root "app" da imagem oficial.
RUN mkdir -p /app/data/uploads && chown -R app:app /app/data
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Zalek360.Api.dll"]
