FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj DouyinContentGenerator.Core/
COPY src/DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj DouyinContentGenerator.Infrastructure/
COPY src/DouyinContentGenerator.API/DouyinContentGenerator.API.csproj DouyinContentGenerator.API/

RUN dotnet restore DouyinContentGenerator.API/DouyinContentGenerator.API.csproj

COPY src/ .
RUN dotnet publish DouyinContentGenerator.API/DouyinContentGenerator.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DouyinContentGenerator.API.dll"]
