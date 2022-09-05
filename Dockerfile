#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim-amd64 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# build, test, and publish application binaries
# note: this needs to be pinned to an amd64 image in order to publish armv7 binaries
# https://github.com/dotnet/dotnet-docker/issues/1537#issuecomment-615269150
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim-amd64 AS build
WORKDIR /src
COPY ["iCalGrabber.csproj", "."]
RUN dotnet restore "./iCalGrabber.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "iCalGrabber.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "iCalGrabber.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "iCalGrabber.dll"]
