FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["*.sln", "./"]
COPY ["*/*.csproj", "./"]
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore
COPY . .
WORKDIR "/src/NoizeBot"
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
RUN apt-get update \
 && DEBIAN_FRONTEND=noninteractive apt-get install -y mpg123 festival \
 && rm -rf /var/lib/apt/lists/*
RUN ln -s /dev/stdout /var/local/pico2wave.wav 
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "NoizeBot.dll"]
