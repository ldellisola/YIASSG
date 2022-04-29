FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

RUN apt-get update
RUN apt-get -y install git

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["YIASSG.BackgroundWorker/YIASSG.BackgroundWorker.csproj", "YIASSG.BackgroundWorker/"]
COPY ["YIASSG/YIASSG.csproj", "YIASSG/"]
RUN dotnet restore "YIASSG.BackgroundWorker/YIASSG.BackgroundWorker.csproj"
COPY . .
WORKDIR "/src/YIASSG.BackgroundWorker"
RUN dotnet build "YIASSG.BackgroundWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "YIASSG.BackgroundWorker.csproj" -c Release -o /app/publish
RUN mkdir "/data" 
    
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YIASSG.BackgroundWorker.dll"]
