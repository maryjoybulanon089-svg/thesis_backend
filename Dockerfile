# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY . ./
RUN dotnet restore "ThesisRepository.csproj"

# Publish
RUN dotnet publish "ThesisRepository.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# Entrypoint script will set ASPNETCORE_URLS from the PORT env variable if present
COPY entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/app/entrypoint.sh"]
