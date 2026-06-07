# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ProjectManagerWebAPI.csproj", "./"]
RUN dotnet restore "ProjectManagerWebAPI.csproj"

COPY . .
RUN dotnet build "ProjectManagerWebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectManagerWebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ProjectManagerWebAPI.dll"]
