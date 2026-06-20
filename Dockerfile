# Cockpit-Image: SPOT-Root wird als Volume /data gemountet.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Cdd.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
VOLUME /data
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cdd.Web.dll", "--root", "/data"]
