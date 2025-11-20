namespace WebScraperVideojuegos.Services;

using Microsoft.Extensions.Logging;
using WebScraperVideojuegos.Models;

public class JuegoDataService
{
    private readonly ILogger<JuegoDataService> _logger;

    public JuegoDataService(ILogger<JuegoDataService> logger)
    {
        _logger = logger;
    }

    public List<string> CargarNombresJuegos(string archivo = "K:\\Code\\C#\\WebScraperVideojuegos\\Data\\juegos.txt")
    {
        var juegos = new List<string>();

        try
        {
            if (File.Exists(archivo))
            {
                var lineas = File.ReadAllLines(archivo);
                juegos = lineas.Where(l => !string.IsNullOrWhiteSpace(l))
                              .Select(l => l.Trim())
                              .ToList();

                _logger.LogInformation("✅ Cargados {Count} juegos desde {Archivo}", juegos.Count, archivo);
            }
            else
            {
                _logger.LogWarning("⚠️ Archivo {Archivo} no encontrado, usando lista por defecto", archivo);
                juegos = CargarListaPorDefecto();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error cargando juegos desde {Archivo}", archivo);
            juegos = CargarListaPorDefecto();
        }

        return juegos;
    }

    private List<string> CargarListaPorDefecto()
    {
        return new List<string>
        {
            "The Witcher 3: Wild Hunt",
            "Cyberpunk 2077",
            "Elden Ring",
            "Red Dead Redemption 2",
            "Grand Theft Auto V",
            "Baldur's Gate 3",
            "Starfield",
            "Hogwarts Legacy",
            "Call of Duty: Modern Warfare III",
            "FIFA 24",
            "Assassin's Creed Mirage",
            "Marvel's Spider-Man: Miles Morales",
            "God of War",
            "The Last of Us Part I",
            "Horizon Forbidden West",
            "Final Fantasy XVI",
            "Resident Evil 4",
            "Dead Space",
            "Street Fighter 6",
            "Diablo IV"
        };
    }
}