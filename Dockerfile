# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

WORKDIR /src/src/DecisionOS.Distribution.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

# Railway injects PORT; default keeps local docker run easy.
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
ENV DataProtection__Path=/var/dpkeys
RUN mkdir -p /var/dpkeys
EXPOSE 8080

ENTRYPOINT ["dotnet", "DecisionOS.Distribution.Web.dll"]
