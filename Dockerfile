FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/TaskFlow.Api/TaskFlow.Api.csproj src/TaskFlow.Api/
RUN dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data && chown -R 1000:1000 /app/data
ENV ASPNETCORE_URLS=http://+:10000 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/app/data/taskflow.db" \
    TASKFLOW_JWT_KEY="demo-only-not-a-secret-taskflow-hf-space-signing-key-2026"
EXPOSE 10000
USER 1000
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]