FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY StorageMesh.Server/StorageMesh.Server.csproj StorageMesh.Server/
RUN dotnet restore StorageMesh.Server/StorageMesh.Server.csproj
COPY StorageMesh.Server/ StorageMesh.Server/
WORKDIR /app/StorageMesh.Server
RUN dotnet publish \
    -c Release \
    -o /app/publish \
    --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "StorageMesh.Server.dll"]