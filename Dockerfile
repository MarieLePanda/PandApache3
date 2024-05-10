FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
RUN mkdir -p /etc/PandApache3/www
RUN mkdir -p /etc/PandApache3/conf
RUN mkdir -p /var/log/PandApache3

COPY www /etc/PandApache3/www
COPY conf /etc/PandApache3/conf
#COPY htpasswd.txt /etc/PandApache3/

CMD ["dotnet", "PandApache3.dll"]