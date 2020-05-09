# retrieve git commit information
FROM alpine:latest AS git
COPY .git ./.git

RUN apk add --no-cache git
RUN git log -1 --date=iso-strict > version.txt

# build backend
FROM mcr.microsoft.com/dotnet/core/sdk:latest AS idx
WORKDIR /app

COPY nhitomi-idx/nhitomi/nhitomi.csproj ./
RUN dotnet restore

COPY nhitomi-idx/nhitomi ./
RUN dotnet build -c Release -o build --no-restore

# generate api spec file
RUN dotnet "build/nhitomi.dll" -- --generate-spec > apispec.json

# build frontend
FROM node:lts-alpine AS web
WORKDIR /app

COPY nhitomi-web/package*.json ./
RUN npm install

COPY --from=idx /app/apispec.json ./
COPY nhitomi-web/genclient.js ./

# java 8 required by openapi-generator
RUN apk add --no-cache openjdk8-jre
RUN npm run genclient apispec.json

COPY nhitomi-web ./
COPY locales ../locales/
RUN npm run build

# build final image
FROM mcr.microsoft.com/dotnet/core/aspnet:latest as app
WORKDIR /app

# fontconfig required by SkiaSharp
RUN apt-get update && apt-get install -y fontconfig

COPY --from=idx /app/build ./
COPY --from=web /app/build ./static/
COPY --from=git version.txt ./

ENV ASPNETCORE_ENVIRONMENT Production
ENTRYPOINT ["dotnet", "nhitomi.dll"]
