FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend.csproj", "./"]
RUN dotnet restore "backend.csproj"

COPY . .
RUN dotnet publish "backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002

HEALTHCHECK --interval=15s --timeout=10s --retries=5 --start-period=20s \
  CMD curl -f http://localhost:5002/health || exit 1

ENTRYPOINT ["dotnet", "backend.dll"]
