# Use the .NET 9.0 SDK preview image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build-env
WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the code and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime image with .NET 9.0 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview
WORKDIR /app
COPY --from=build-env /app/out ./

# Optionally set your entrypoint if your DLL name is Pinpoint9.dll
# ENTRYPOINT ["dotnet", "Pinpoint 9.dll"]
