# syntax=docker/dockerfile:1
# escape=`

FROM mcr.microsoft.com/dotnet/sdk:8.0.203-jammy-amd64@sha256:f1fec9720c4d6cc1067aec5516e3742f33e7944baf12b28f7b1850b6f578daed AS build-env

LABEL description="This is TodoApi layer image of whole TODOAPI-MAIN application. `
It is responsible among others for authorization, authentication and database."

RUN useradd --system --user-group kocurc
USER kocurc

ARG OUTPUT_PATH
ENV OUTPUT_PATH=$OUTPUT_PATH

RUN mkdir /TodoApi
COPY TodoApi TodoApi

RUN dotnet publish -c Release -o OUTPUT_PATH ./TodoApi/TodoApi.csproj


FROM mcr.microsoft.com/dotnet/aspnet@sha256:69f5151d295791d85543e75905e69b6b74e3f0babd8a41583d8b06c99e759f4f

ARG OUTPUT_PATH
ENV OUTPUT_PATH=$OUTPUT_PATH

COPY --from=build-env OUTPUT_PATH OUTPUT_PATH
ENTRYPOINT ["dotnet"]
CMD ["/Out/TodoApi"]

HEALTHCHECK --interval=5m --timeout=30s --start-period=30s --retries=3 `
    CMD powershell ./Scripts/HealthCheck.ps1
