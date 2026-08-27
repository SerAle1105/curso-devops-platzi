# ---------- Etapa 1: compilar ----------
# Imagen con el SDK completo de .NET 8: sirve para compilar, pero pesa ~800 MB.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos primero SOLO el .csproj para aprovechar la caché de Docker:
# mientras no cambien las dependencias, esta capa no se vuelve a ejecutar.
COPY BmiApi.csproj .
RUN dotnet restore

# Ahora sí el resto del código fuente.
COPY . .

# publish ya compila en Release; no hace falta un "dotnet build" aparte.
RUN dotnet publish -c Release -o /app --no-restore

# ---------- Etapa 2: ejecutar ----------
# Imagen solo de runtime (~220 MB): no lleva compilador ni código fuente.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Traemos únicamente el resultado publicado de la etapa anterior.
COPY --from=build /app .

# Las imágenes de .NET 8 escuchan en el 8080 por defecto.
EXPOSE 8080

ENTRYPOINT ["dotnet", "BmiApi.dll"]
