# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Common/Common.csproj", "Common/"]
COPY ["src/TaskScheduler/TaskScheduler.csproj", "TaskScheduler/"]
RUN dotnet restore "TaskScheduler/TaskScheduler.csproj"

# Copy everything else and build
COPY src/Common/ Common/
COPY src/TaskScheduler/ TaskScheduler/
WORKDIR "/src/TaskScheduler"
RUN dotnet build "TaskScheduler.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TaskScheduler.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use Azure Functions base image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
WORKDIR /home/site/wwwroot

# Azure Functions configuration
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY --from=publish /app/publish .
