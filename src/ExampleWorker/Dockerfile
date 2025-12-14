# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj file and restore dependencies
COPY ["src/ExampleWorker/ExampleWorker.csproj", "ExampleWorker/"]
RUN dotnet restore "ExampleWorker/ExampleWorker.csproj"

# Copy everything else and build
COPY src/ExampleWorker/ ExampleWorker/
WORKDIR "/src/ExampleWorker"
RUN dotnet build "ExampleWorker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ExampleWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExampleWorker.dll"]

