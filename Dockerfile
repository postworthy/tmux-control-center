# syntax=docker/dockerfile:1.7

FROM node:20-bookworm-slim AS web-build
WORKDIR /source
COPY src/TmuxMobile.Web/package.json src/TmuxMobile.Web/package-lock.json src/TmuxMobile.Web/
RUN npm --prefix src/TmuxMobile.Web ci
COPY src/TmuxMobile.Web src/TmuxMobile.Web
RUN npm --prefix src/TmuxMobile.Web run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS server-build
RUN apt-get update \
    && apt-get install --yes --no-install-recommends gcc libc6-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /source
COPY . .
RUN dotnet restore TmuxMobile.sln
RUN find src/TmuxMobile.Server/wwwroot -mindepth 1 -delete
COPY --from=web-build /source/src/TmuxMobile.Server/wwwroot src/TmuxMobile.Server/wwwroot
RUN dotnet publish src/TmuxMobile.Server/TmuxMobile.Server.csproj \
    --configuration Release \
    --no-restore \
    --output /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends ca-certificates curl tmux \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=server-build /publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    Urls=https://0.0.0.0:5443 \
    DOTNET_EnableDiagnostics=0

EXPOSE 5443
ENTRYPOINT ["dotnet", "TmuxMobile.Server.dll"]
