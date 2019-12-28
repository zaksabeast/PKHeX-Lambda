FROM mcr.microsoft.com/dotnet/core/sdk:2.1

ENV PATH "$PATH:/root/.dotnet/tools"

RUN apt-get update && \
  apt-get install zip --no-install-recommends -y && \
  dotnet tool install --global Amazon.Lambda.Tools
