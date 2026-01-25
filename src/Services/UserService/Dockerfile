FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["src/Services/UserService/UserService.csproj", "Services/UserService/"]
RUN dotnet restore "Services/UserService/UserService.csproj"
COPY src/Shared.Contracts/ Shared.Contracts/
COPY src/Services/UserService/ Services/UserService/
WORKDIR "/src/Services/UserService"
RUN dotnet build "UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]
