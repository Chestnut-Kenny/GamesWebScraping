// Program.cs
namespace WebScraperVideojuegos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebScraperVideojuegos.Interfaces;
using WebScraperVideojuegos.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                services.AddSingleton<ScrapingManager>();
                services.AddSingleton<HtmlGenerator>();
                services.AddSingleton<JuegoDataService>();
                services.AddSingleton<CurrencyService>();

                // Registrar scrapers
                services.AddTransient<IScraper, BusquedaScraper>();
                services.AddTransient<IScraper, SteamScraper>();
                services.AddTransient<IScraper, EpicGamesScraper>();
                services.AddTransient<IScraper, GogScraper>();


                // Agregar más scrapers aquí...
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Iniciando aplicación de scraping...");

            var scrapingManager = host.Services.GetRequiredService<ScrapingManager>();
            var htmlGenerator = host.Services.GetRequiredService<HtmlGenerator>();

            // Ejecutar scraping paralelo
            var juegos = await scrapingManager.EjecutarScrapingParaleloAsync();

            // -------------------------------
            // Generar HTML principal
            // -------------------------------
            var html = htmlGenerator.GenerarPaginaWeb(juegos);

            // Guardar versión histórica
            var nombreArchivo = $"ofertas-videojuegos-{DateTime.Now:yyyyMMdd-HHmmss}.html";
            await File.WriteAllTextAsync(nombreArchivo, html);

            // Guardar como index.html
            await File.WriteAllTextAsync("index.html", html);

            // -------------------------------
            // Crear carpeta para páginas individuales
            // -------------------------------
            Directory.CreateDirectory("juegos");

            // -------------------------------
            // Generar páginas individuales
            // -------------------------------
            foreach (var juego in juegos)
            {
                string paginaJuego = htmlGenerator.GenerarPaginaJuego(juego);
                string ruta = $"juegos/{juego.Id}.html";
                await File.WriteAllTextAsync(ruta, paginaJuego);
            }

            logger.LogInformation("Página HTML generada: {Archivo}", nombreArchivo);
            logger.LogInformation("Páginas individuales generadas correctamente.");

            logger.LogInformation("Proceso completado exitosamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en la aplicación");
        }
    }
}