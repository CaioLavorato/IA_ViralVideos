FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY VideoSaaS.sln global.json ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Workers/Workers.csproj src/Workers/
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj
COPY . .
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

FROM docker:27-cli AS docker-cli

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=docker-cli /usr/local/bin/docker /usr/local/bin/docker
USER root
RUN mkdir -p /app/data /app/media /app/models/piper
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
