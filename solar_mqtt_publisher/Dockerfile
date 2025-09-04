ARG BUILD_FROM=ghcr.io/home-assistant/amd64-base:3.19

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETARCH
WORKDIR /src

COPY /src ./
RUN dotnet restore SolarMqtt.csproj
RUN case "$TARGETARCH" in \
    amd64) rid=linux-musl-x64;; \
    arm64) rid=linux-musl-arm64;; \
    armv7) rid=linux-musleabihf;; \
    *) rid=linux-musl-x64;; \
  esac \
  && dotnet publish SolarMqtt.csproj -c Release -r $rid \
     --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true \
     -o /out

# --- Runtime stage ---
FROM ${BUILD_FROM}
WORKDIR /app

RUN apk add --no-cache ca-certificates tzdata && update-ca-certificates

COPY --from=build /out/ /app/
COPY run.sh /app/
RUN chmod +x /app/run.sh

CMD ["/app/run.sh"]
