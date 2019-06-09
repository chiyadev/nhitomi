# Build stage
FROM microsoft/dotnet:sdk AS build
WORKDIR /

# Copy everything
COPY . ./

# Publish project
RUN dotnet publish nhitomi -c Release -o ./bin

# Runtime stage
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /

# Copy publish output
COPY --from=build /nhitomi/bin .

# Expose ports
EXPOSE 80

# Entrypoint
ENTRYPOINT ["dotnet", "nhitomi.dll"]
