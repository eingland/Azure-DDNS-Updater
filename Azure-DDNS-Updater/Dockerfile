FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["Azure-DDNS-Updater/Azure-DDNS-Updater.csproj", "Azure-DDNS-Updater/"]
RUN dotnet restore "Azure-DDNS-Updater/Azure-DDNS-Updater.csproj"
COPY . .
WORKDIR "/src/Azure-DDNS-Updater"
RUN dotnet build "Azure-DDNS-Updater.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Azure-DDNS-Updater.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Azure-DDNS-Updater.dll"]