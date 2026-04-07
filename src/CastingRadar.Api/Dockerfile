FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG CONFIGURATION=Release
WORKDIR /src
# Install dotnet-watch for debug hot-reload support
RUN dotnet tool install --global dotnet-watch 2>/dev/null || true
ENV PATH="$PATH:/root/.dotnet/tools"
COPY ["src/CastingRadar.Api/CastingRadar.Api.csproj", "src/CastingRadar.Api/"]
COPY ["src/CastingRadar.Infrastructure/CastingRadar.Infrastructure.csproj", "src/CastingRadar.Infrastructure/"]
COPY ["src/CastingRadar.Application/CastingRadar.Application.csproj", "src/CastingRadar.Application/"]
COPY ["src/CastingRadar.Domain/CastingRadar.Domain.csproj", "src/CastingRadar.Domain/"]
RUN dotnet restore "src/CastingRadar.Api/CastingRadar.Api.csproj"
COPY . .
WORKDIR "/src/src/CastingRadar.Api"
RUN dotnet build "CastingRadar.Api.csproj" -c ${CONFIGURATION} -o /app/build

FROM build AS publish
RUN dotnet publish "CastingRadar.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CastingRadar.Api.dll"]
