FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY StorageMesh.Server/StorageMesh.Server.csproj StorageMesh.Server/
RUN dotnet restore StorageMesh.Server/StorageMesh.Server.csproj

COPY StorageMesh.Server/ StorageMesh.Server/

WORKDIR /app/StorageMesh.Server

EXPOSE 8080

CMD ["dotnet", "run", "--urls", "http://0.0.0.0:8080"]