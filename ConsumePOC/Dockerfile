FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app
COPY ./ConsumePOC /ConsumePOC
COPY ./RabbitMQClientInfrastructure /RabbitMQClientInfrastructure
COPY ./QueTestIntegration /QueTestIntegration

WORKDIR /QueTestIntegration
RUN dotnet restore
RUN dotnet build --configuration Release

WORKDIR /RabbitMQClientInfrastructure
RUN dotnet restore
RUN dotnet build --configuration Release

WORKDIR /ConsumePOC
RUN dotnet restore
RUN dotnet build --configuration Release

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS runtime
COPY --from=build /ConsumePOC/bin/Release/netcoreapp3.1 app/

WORKDIR /app
ENTRYPOINT  [ "dotnet" ,"ConsumePOC.dll" ]  