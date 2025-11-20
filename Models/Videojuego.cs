// Models/Videojuego.cs
namespace WebScraperVideojuegos.Models;
public class Videojuego
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public decimal PrecioOriginal { get; set; }
    public decimal PrecioDescuento { get; set; }
    public int PorcentajeDescuento { get; set; }
    public List<TiendaPrecio> PreciosTiendas { get; set; } = new();
    public List<string> Plataformas { get; set; } = new();
    public decimal Calificacion { get; set; }
    public int TotalReseñas { get; set; }
    public string TiempoCompletar { get; set; }
    public string UrlImagen { get; set; }
    public string UrlProducto { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.Now;

    public string Tipo { get; set; } = "otro";
    public string Formato { get; set; } = "otro";

    public string TiempoMain { get; set; } = "";
    public string TiempoMainExtra { get; set; } = "";
    public string TiempoCompletionist { get; set; } = "";
    public string TiempoHowLongToBeat =>
        $"Main: {TiempoMain} | Main+Extra: {TiempoMainExtra} | Completionist: {TiempoCompletionist}";

}

// Models/TiendaPrecio.cs
public class TiendaPrecio
{
    public string NombreTienda { get; set; }
    public decimal Precio { get; set; }  // Siempre en USD para comparar
    public decimal PrecioOriginal { get; set; }  // Siempre en USD
    public int Descuento { get; set; }
    public string UrlTienda { get; set; }

    // Nuevas propiedades para precios originales
    public string MonedaOriginal { get; set; } = "USD";
    public decimal? PrecioCRC { get; set; }  // Precio real en colones
    public decimal? PrecioOriginalCRC { get; set; }  // Original en colones
}

// Models/ScrapingResult.cs
public class ScrapingResult
{
    public bool Success { get; set; }
    public List<Videojuego> Videojuegos { get; set; } = new();
    public string Error { get; set; }
    public string Fuente { get; set; }
    public TimeSpan TiempoProcesamiento { get; set; }
}