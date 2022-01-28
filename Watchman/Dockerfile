FROM mcr.microsoft.com/dotnet/runtime:6.0-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /src
COPY ["Watchman.csproj", "./"]
RUN dotnet restore "Watchman.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Watchman.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Watchman.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app/publish
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Watchman.dll"]
