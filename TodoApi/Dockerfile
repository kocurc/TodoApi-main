
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
# Copy everything to image
COPY . ./

# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish -c Release -o Out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /TodoApi
COPY --from=build-env /TodoApi/out .
ENTRYPOINT ["dotnet", "TodoApi"]
