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

FROM ubuntu:noble AS tmux-build
ARG TMUX_VERSION=3.4
RUN case "$TMUX_VERSION" in \
      ''|*[!0-9A-Za-z.-]*) echo "Invalid TMUX_VERSION" >&2; exit 1 ;; \
    esac \
    && apt-get update \
    && apt-get install --yes --no-install-recommends \
      bison build-essential ca-certificates curl libevent-dev libncurses-dev pkg-config \
    && curl --proto '=https' --tlsv1.2 --fail --show-error --silent --location \
      --output /tmp/tmux.tar.gz \
      "https://github.com/tmux/tmux/releases/download/${TMUX_VERSION}/tmux-${TMUX_VERSION}.tar.gz" \
    && mkdir /tmp/tmux-source \
    && tar --extract --gzip --file /tmp/tmux.tar.gz --directory /tmp/tmux-source --strip-components=1 \
    && cd /tmp/tmux-source \
    && ./configure --prefix=/opt/tmux \
    && make -j2 \
    && make install \
    && test "$(/opt/tmux/bin/tmux -V)" = "tmux ${TMUX_VERSION}"

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
ARG TMUX_VERSION=3.4
RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
      ca-certificates curl libevent-core-2.1-7t64 libncursesw6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=server-build /publish .
COPY --from=tmux-build /opt/tmux/bin/tmux /usr/bin/tmux
RUN test "$(/usr/bin/tmux -V)" = "tmux ${TMUX_VERSION}"

ENV ASPNETCORE_ENVIRONMENT=Production \
    Urls=https://0.0.0.0:5443 \
    DOTNET_EnableDiagnostics=0

EXPOSE 5443
ENTRYPOINT ["dotnet", "TmuxMobile.Server.dll"]
