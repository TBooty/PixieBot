FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o out

# Install cultures (same approach as Alpine SDK image)


FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine as runtime
COPY --from=build /app/out /
RUN apk add --no-cache icu-libs
# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENTRYPOINT [ "dotnet", "PixieBot.dll" ]