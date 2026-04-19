# Stage 1: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Coffee.sln", "./"]
COPY ["Coffee/Coffee.csproj", "Coffee/"]
RUN dotnet restore

COPY . .
WORKDIR "/src/Coffee"
RUN dotnet build "Coffee.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "Coffee.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Coffee.dll"]