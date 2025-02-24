# Use the .NET 9.0 SDK image (update tag as needed)
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy the remaining files and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image using .NET 9.0 runtime (if available)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "Pinpoint 9.dll"]
