FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

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
mkdir "/data" 
    
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YIASSG.BackgroundWorker.dll"]
