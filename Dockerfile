FROM mcr.microsoft.com/dotnet/runtime:9.0-noble-chiseled AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipFunc/ClipFunc.csproj", "ClipFunc/"]
RUN dotnet tool restore
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet restore "ClipFunc/ClipFunc.csproj"
COPY . .
WORKDIR "/src/ClipFunc"
RUN dotnet build "./ClipFunc.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClipFunc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final-cop
WORKDIR /app
COPY --from=publish /app/publish .

FROM final-cop AS final
ENV DOTNET_ENVIRONMENT="Production"
WORKDIR /
ENTRYPOINT ["dotnet", "/app/ClipFunc.dll"]
