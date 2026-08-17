# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Pireon.API/*.csproj ./Pireon.API/
RUN dotnet restore ./Pireon.API/Pireon.API.csproj

COPY Pireon.API/. ./Pireon.API/
RUN dotnet publish ./Pireon.API/Pireon.API.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# LibreOffice para convertir el formulario de entrega a PDF
RUN apt-get update \
    && apt-get install -y --no-install-recommends libreoffice-writer fonts-dejavu-core fonts-liberation2 fonts-crosextra-carlito fonts-crosextra-caladea \
    && rm -rf /var/lib/apt/lists/*

ENV DELIVERY_FORM_SOFFICE_PATH=/usr/bin/soffice

COPY --from=build /out .

# Carpeta para persistir el archivo .db
VOLUME /app/data

EXPOSE 80
ENTRYPOINT ["dotnet", "Pireon.API.dll"]
