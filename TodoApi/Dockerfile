# Base image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# Set variable for output path: docker build --build-arg OUTPUT_PATH='/Out' --tag todo-api .
ARG OUTPUT_PATH
ENV OUTPUT_PATH=$OUTPUT_PATH

# Copy TodoApi project files
RUN mkdir --parents /TodoApi
COPY TodoApi TodoApi

# List files in TodoApi directory
RUN ls /TodoApi

# Build and publish
RUN dotnet publish -c Release -o Out ./TodoApi/TodoApi.csproj

# Prepare runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
ARG OUTPUT_PATH
ENV OUTPUT_PATH=$OUTPUT_PATH
COPY --from=build-env /Out /Out
ENTRYPOINT ["dotnet", "/Out/TodoApi"]
