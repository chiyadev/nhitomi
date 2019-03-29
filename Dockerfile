FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY nhitomi/nhitomi.csproj ./nhitomi/
COPY nhitomi.Core/nhitomi.Core.csproj ./nhitomi.Core/

RUN dotnet restore ./nhitomi/nhitomi.csproj

# Copy everything else and build
COPY nhitomi ./nhitomi/
COPY nhitomi.Core ./nhitomi.Core/

RUN dotnet publish ./nhitomi/nhitomi.csproj -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "nhitomi.dll"]
