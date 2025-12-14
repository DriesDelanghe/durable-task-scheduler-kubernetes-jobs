# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Common/Common.csproj", "Common/"]
COPY ["src/k8sController/k8sController.csproj", "k8sController/"]
RUN dotnet restore "k8sController/k8sController.csproj"

# Copy everything else and build
COPY src/Common/ Common/
COPY src/k8sController/ k8sController/
WORKDIR "/src/k8sController"
RUN dotnet build "k8sController.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "k8sController.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "k8sController.dll"]

