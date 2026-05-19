# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartNotes.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create folder for SQLite database volume
RUN mkdir -p /app/data

COPY --from=build /app/publish .

# SQLite db goes in /app/data (mounted as a volume in production)
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/smartnotes.db"

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SmartNotes.dll"]
