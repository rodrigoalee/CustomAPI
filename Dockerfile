# [INICIO][31/8/2026][Rodriale][Compilación en dos etapas: la imagen final solo lleva el runtime, no el SDK completo]

# --- Etapa 1: compilar ---
# Esta imagen trae el SDK de .NET 8 completo (compilador incluido) para poder publicar el proyecto.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Se copia primero solo el csproj para que Docker cachee el restore:
# mientras no cambien los paquetes, no vuelve a descargarlos en cada build.
COPY CustomCore.API/CustomCore.API.csproj CustomCore.API/
RUN dotnet restore CustomCore.API/CustomCore.API.csproj

# Ahora sí el resto del código fuente.
COPY . .
RUN dotnet publish CustomCore.API/CustomCore.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Etapa 2: ejecutar ---
# Solo el runtime: sin compilador la imagen final baja de ~800 MB a ~220 MB.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Render enruta el tráfico a este puerto. Sin esto la app escucha en el 5000 y nadie la encuentra.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CustomCore.API.dll"]

# [FIN][31/8/2026][Rodriale][Compilación en dos etapas]
