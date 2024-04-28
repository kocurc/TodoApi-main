# syntax=docker/dockerfile:1

# Base image
FROM mcr.microsoft.com/dotnet/sdk:8.0.203-jammy-amd64@sha256:f1fec9720c4d6cc1067aec5516e3742f33e7944baf12b28f7b1850b6f578daed AS build-env

LABEL description="This is TodoApi layer image of whole TODOAPI-MAIN application. \
It is responsible among others for authorization, authentication and database."

RUN net user /add kocurc
USER kocurc

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
FROM mcr.microsoft.com/dotnet/aspnet@sha256:69f5151d295791d85543e75905e69b6b74e3f0babd8a41583d8b06c99e759f4f
ARG OUTPUT_PATH
ENV OUTPUT_PATH=$OUTPUT_PATH
COPY --from=build-env /Out /Out
ENTRYPOINT ["dotnet"]
CMD ["/Out/TodoApi"]

HEALTHCHECK --interval=5m --timeout=30s --start-period=30s --retries=3 \
    CMD powershell -command `  
        try { `
        $response = Invoke-WebRequest https://localhost:5001; `
        if ($response.StatusCode -eq 200) { return 0} `
        else {return 1}; `
        } catch { return 1 }
