# Games Web Scraping

Un proyecto en C#, Selenium y HtmlAgilityPack que scrapea tiendas de videojuegos, genera una página web completa con filtros avanzados y crea páginas individuales para cada juego con datos detallados como precios, plataformas, Metacritic y HowLongToBeat.

## Descripción

Web Scraper Videojuegos es una aplicación de consola desarrollada en C# que realiza scraping de las principales tiendas digitales de videojuegos (Steam, Epic Games Store y GOG) para obtener información actualizada sobre ofertas, precios y descuentos. Genera una página web estática con una interfaz moderna y responsive que permite comparar precios fácilmente.

## Características
- Scraping automatizado de múltiples tiendas

- Comparación de precios en tiempo real

- Detección automática de mejores ofertas

- Filtros avanzados por tipo, plataforma, género y tienda

- Interfaz moderna y responsive

- Compatible con dispositivos móviles

- Conversión de divisas automática (CRC → USD)

- Tiempo de completado desde HowLongToBeat

- Calificaciones y reseñas de Steam

- Géneros extraídos de cada plataforma

## Instalación
### Prerrequisitos

- .NET 6.0 SDK o superior
- Visual Studio 2022

## Pasos de instalación

### 1. Clonar el repositorio
```
git clone https://github.com/Chestnut-Kenny/GamesWebScraping.git
cd GamesWebScraping
```
### 2. Restaurar dependencias
```
dotnet restore
```
### 3. Compilar el proyecto
```
dotnet build
```
### 4. Ejecutar la aplicación
```
dotnet run
```
## Uso
### Ejecución básica
```
dotnet run
```
La aplicación ejecutará automáticamente el scraping de las tres tiendas y generará:

- index.html - Página principal con todos los juegos
- Carpeta juegos/ - Páginas individuales para cada juego

## Configuración
Puedes personalizar los scrapers activos en Program.cs:
```
// Registrar scrapers
services.AddTransient<IScraper, SteamScraper>();
services.AddTransient<IScraper, EpicGamesScraper>();
services.AddTransient<IScraper, GogScraper>();
// services.AddTransient<IScraper, BusquedaScraper>(); // Opcional
```

## Packages

- HtmlAgilityPack - Parsing HTML
- System.Text.Json - Manejo de JSON
- HttpClient - Peticiones HTTP

## APIs Externas

- Steam Web API - Precios y datos de juegos
- Epic Games Store API - Juegos gratuitos y ofertas
- GOG.com API - Catálogo y ofertas
- HowLongToBeat API - Tiempo de completado
- ExchangeRate API - Conversión de divisas
- Metacritic API - Rating de juegos


## Estructura del Proyecto

```
WebScraperVideojuegos/
├── Data/
│   └── juegos.txt               # Repositorio manual de juegos
├── Interfaces/
│   └── IScraper.cs              # Interface para scrapers
├── Models/
│   ├── Videojuego.cs            # Modelo de videojuego
├── Services/
│   ├── ScrapingManager.cs       # Gestión de scrapers
│   ├── HtmlGenerator.cs         # Generación de HTML
│   └── JuegoDataService.cs      # Servicio de datos
└── Program.cs                    # Punto de entrada
```


## Autores

- **Kendall Andrade** - ***Desarrollador***
- **Fabian Rodriguez** - ***Desarrollador***
- **Gabriel Cabrera** - ***Desarrollador***

## 📝 Licencia
Este proyecto está bajo la Licencia MIT. Ver el archivo LICENSE para más detalles.








