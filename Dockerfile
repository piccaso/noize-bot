FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["*.sln", "./"]
COPY ["*/*.csproj", "./"]
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore && dotnet build
COPY . .
WORKDIR "/src/NoizeBot"
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
RUN apt-get update \
 && DEBIAN_FRONTEND=noninteractive apt-get install -y mpg123 festival curl python3 ffmpeg \
 && rm -rf /var/lib/apt/lists/*
RUN curl -sL https://yt-dl.org/downloads/latest/youtube-dl -o /usr/local/bin/youtube-dl && chmod a+rx /usr/local/bin/youtube-dl
RUN ln -s /usr/bin/python3 /usr/bin/python
COPY play-youtube.sh /usr/local/bin/play-youtube
RUN chmod a+rx /usr/local/bin/play-youtube
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "NoizeBot.dll"]
