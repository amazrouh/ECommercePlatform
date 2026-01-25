FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["src/Services/OrderService/OrderService.csproj", "Services/OrderService/"]
RUN dotnet restore "Services/OrderService/OrderService.csproj"
COPY src/Shared.Contracts/ Shared.Contracts/
COPY src/Services/OrderService/ Services/OrderService/
WORKDIR "/src/Services/OrderService"
RUN dotnet build "OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
