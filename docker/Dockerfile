# Multi-stage Dockerfile for building and running a .NET 10 ASP.NET Core app
# - Build stage uses the official .NET 10 SDK image to restore, build, and publish
# - Runtime stage uses the smaller ASP.NET Core runtime image
# - Adjust APP_DIR if your project is in a different path within the context

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj(s) and restore as a separate step to leverage build cache
COPY *.sln ./
COPY *.csproj ./
# If your project uses subfolders, uncomment and adjust the next line
# COPY MyProject/*.csproj ./MyProject/

RUN dotnet restore --verbosity minimal

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Optional: set ASPNETCORE_URLS to listen on port 80
ENV ASPNETCORE_URLS=http://+:80

# Copy published output from build stage
COPY --from=build /app/publish .

# Use an unprivileged port 80 in container; map to desired host port at runtime
EXPOSE 80

ENTRYPOINT ["dotnet", "NetForge.dll"]
