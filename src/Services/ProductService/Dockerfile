FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["src/Services/ProductService/ProductService.csproj", "Services/ProductService/"]
RUN dotnet restore "Services/ProductService/ProductService.csproj"
COPY src/Shared.Contracts/ Shared.Contracts/
COPY src/Services/ProductService/ Services/ProductService/
WORKDIR "/src/Services/ProductService"
RUN dotnet build "ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
