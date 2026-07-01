# ── Stage 1: Build ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore (cached layer if no csproj changes)
COPY ["PulseHR.Api/PulseHR.Api.csproj", "PulseHR.Api/"]
RUN dotnet restore "PulseHR.Api/PulseHR.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/PulseHR.Api"
RUN dotnet build "PulseHR.Api.csproj" -c Release -o /app/build

# ── Stage 2: Publish ─────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "PulseHR.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 3: Runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Render uses port 8080 by default
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PulseHR.Api.dll"]
