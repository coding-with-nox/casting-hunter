FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG CONFIGURATION=Release
WORKDIR /src
RUN dotnet tool install --global dotnet-watch 2>/dev/null || true
ENV PATH="$PATH:/root/.dotnet/tools"
COPY ["src/CastingRadar.Worker/CastingRadar.Worker.csproj", "src/CastingRadar.Worker/"]
COPY ["src/CastingRadar.Infrastructure/CastingRadar.Infrastructure.csproj", "src/CastingRadar.Infrastructure/"]
COPY ["src/CastingRadar.Application/CastingRadar.Application.csproj", "src/CastingRadar.Application/"]
COPY ["src/CastingRadar.Domain/CastingRadar.Domain.csproj", "src/CastingRadar.Domain/"]
RUN dotnet restore "src/CastingRadar.Worker/CastingRadar.Worker.csproj"
COPY . .
WORKDIR "/src/src/CastingRadar.Worker"
RUN dotnet build "CastingRadar.Worker.csproj" -c ${CONFIGURATION} -o /app/build

FROM build AS publish
RUN dotnet publish "CastingRadar.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CastingRadar.Worker.dll"]
