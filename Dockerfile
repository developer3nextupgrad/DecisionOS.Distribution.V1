# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Go to Web project (MAIN ENTRY PROJECT)
WORKDIR /src/src/DecisionOS.Distribution.Web

# Restore + publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Railway injects PORT; default keeps local docker run easy.
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
ENV DataProtection__Path=/var/dpkeys
RUN mkdir -p /var/dpkeys
EXPOSE 8080

# START CORRECT DLL (THIS FIXES YOUR ERROR)
ENTRYPOINT ["dotnet", "DecisionOS.Distribution.Web.dll"]
