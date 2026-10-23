FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "backend/SyncWaves.RTC/SyncWaves.RTC.Api/SyncWaves.RTC.Api.csproj"
RUN dotnet publish "backend/SyncWaves.RTC/SyncWaves.RTC.Api/SyncWaves.RTC.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SyncWaves.RTC.Api.dll"]
