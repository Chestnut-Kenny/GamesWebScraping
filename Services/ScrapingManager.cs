// Services/ScrapingManager.cs
namespace WebScraperVideojuegos.Services;
using Microsoft.Extensions.Logging;
using WebScraperVideojuegos.Interfaces;
using WebScraperVideojuegos.Models;

public class ScrapingManager
{
    private readonly IEnumerable<IScraper> _scrapers;
    private readonly ILogger<ScrapingManager> _logger;

    public ScrapingManager(IEnumerable<IScraper> scrapers, ILogger<ScrapingManager> logger)
    {
        _scrapers = scrapers;
        _logger = logger;
    }

    public async Task<List<Videojuego>> EjecutarScrapingParaleloAsync()
    {
        _logger.LogInformation("Iniciando proceso de scraping paralelo con {Count} scrapers", _scrapers.Count());

        var tasks = _scrapers.Select(scraper => EjecutarScraperConTimeoutAsync(scraper)).ToList();
        var resultados = await Task.WhenAll(tasks);

        var juegos = resultados.Where(r => r.Success)
                              .SelectMany(r => r.Videojuegos)
                              .ToList();

        _logger.LogInformation("Scraping completado. Se obtuvieron {Count} juegos", juegos.Count);

        // Log de resultados por fuente
        foreach (var resultado in resultados)
        {
            _logger.LogInformation("{Fuente}: {Success} - {Count} juegos - {Tiempo}ms",
                resultado.Fuente, resultado.Success, resultado.Videojuegos.Count,
                resultado.TiempoProcesamiento.TotalMilliseconds);
        }

        return juegos;
    }

    private async Task<ScrapingResult> EjecutarScraperConTimeoutAsync(IScraper scraper)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await scraper.ObtenerOfertasAsync();
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout en scraper {Scraper}", scraper.Nombre);
            return new ScrapingResult
            {
                Success = false,
                Error = "Timeout",
                Fuente = scraper.Nombre
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico en scraper {Scraper}", scraper.Nombre);
            return new ScrapingResult
            {
                Success = false,
                Error = ex.Message,
                Fuente = scraper.Nombre
            };
        }
    }
}
