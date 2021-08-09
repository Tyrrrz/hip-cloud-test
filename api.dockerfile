# ** Build

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY Hipster.Api Hipster.Api

RUN dotnet publish Hipster.Api -o Hipster.Api/artifacts -c Release

# ** Run

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS run
WORKDIR /app

EXPOSE 80
EXPOSE 443

COPY --from=build /src/Hipster.Api/artifacts ./

ENTRYPOINT ["dotnet", "Hipster.Api.dll"]