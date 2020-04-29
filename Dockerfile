### STAGE 1: retrieve information of the git commit we are building against
FROM alpine:latest AS git

COPY .git ./.git

RUN apk add --no-cache git
RUN git log -1 --date=iso-strict > version.txt


### STAGE 2: build backend
FROM mcr.microsoft.com/dotnet/core/sdk:latest AS idx
WORKDIR /app

# copy project and restore as distinct layer
COPY nhitomi-idx/nhitomi/nhitomi.csproj ./

RUN dotnet restore

# build project
COPY nhitomi-idx/nhitomi ./

RUN dotnet build -c Release -o build --no-restore

# generate api specification
RUN dotnet "build/nhitomi.dll" -- --generate-spec > apispec.json


### STAGE 3: build frontend
FROM node:lts-alpine AS web
WORKDIR /app

# install Java 8 (required by openapi-generator)
RUN apk add --no-cache openjdk8-jre

# copy packages and install as distinct layer
COPY nhitomi-web/package.json ./
COPY nhitomi-web/package-lock.json ./

RUN npm install

# generate api client
COPY --from=idx /app/apispec.json ./
COPY nhitomi-web/genclient.js ./

RUN npm run genclient apispec.json

# build project
COPY nhitomi-web ./
COPY locales ../locales/
RUN npm run build


### STAGE 4: final image
FROM mcr.microsoft.com/dotnet/core/aspnet:latest as app
WORKDIR /app

# install fontconfig (required by SkiaSharp)
RUN apt-get update && apt-get install -y fontconfig

# copy backend server
COPY --from=idx /app/build ./

# copy frontend files
COPY --from=web /app/build ./static/

# copy version info
COPY --from=git version.txt ./

ENV ASPNETCORE_ENVIRONMENT Production
ENTRYPOINT ["dotnet", "nhitomi.dll"]
