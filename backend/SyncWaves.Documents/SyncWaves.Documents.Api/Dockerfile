FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "backend/SyncWaves.Documents/SyncWaves.Documents.Api/SyncWaves.Documents.Api.csproj"
RUN dotnet publish "backend/SyncWaves.Documents/SyncWaves.Documents.Api/SyncWaves.Documents.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SyncWaves.Documents.Api.dll"]
