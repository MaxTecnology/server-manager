# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SessionManager.slnx ./
COPY nuget.config ./
COPY src/SessionManager.Domain/ src/SessionManager.Domain/
COPY src/SessionManager.Application/ src/SessionManager.Application/
COPY src/SessionManager.Infrastructure/ src/SessionManager.Infrastructure/
COPY src/SessionManager.WebApi/ src/SessionManager.WebApi/

RUN dotnet restore src/SessionManager.WebApi/SessionManager.WebApi.csproj
RUN dotnet publish src/SessionManager.WebApi/SessionManager.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish/ ./

RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "SessionManager.WebApi.dll"]
