FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TodoApi.sln ./
COPY TodoApi/TodoApi.csproj TodoApi/
COPY TodoApi.Tests/TodoApi.Tests.csproj TodoApi.Tests/
RUN dotnet restore TodoApi/TodoApi.csproj

COPY TodoApi/. TodoApi/
RUN dotnet publish TodoApi/TodoApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /data && chown app:app /data
USER app
COPY --from=build --chown=app:app /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoApi.dll"]
