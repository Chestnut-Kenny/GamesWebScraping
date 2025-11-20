// Interfaces/IScraper.cs
namespace WebScraperVideojuegos.Interfaces;
using HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using WebScraperVideojuegos.Models;
using WebScraperVideojuegos.Services;

public interface IScraper
{
    Task<ScrapingResult> ObtenerOfertasAsync();
    string Nombre { get; }
}

// Scrapers/BaseScraper.cs
public abstract class BaseScraper : IScraper
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger<BaseScraper> _logger;

    public abstract string Nombre { get; }

    protected BaseScraper(HttpClient httpClient, ILogger<BaseScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configurar headers comunes
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public abstract Task<ScrapingResult> ObtenerOfertasAsync();

    protected virtual async Task<string> ObtenerHtmlAsync(string url)
    {
        try
        {
            return await _httpClient.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener HTML de {Url}", url);
            throw;
        }
    }
}

// Scrapers/SteamScraper.cs






public class SteamScraper : IScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SteamScraper> _logger;

    public string Nombre => "Steam";

    public SteamScraper(HttpClient httpClient, ILogger<SteamScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    private async Task<string> ObtenerGeneroReal(string titulo)
    {
        try
        {
            var url = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(titulo)}";
            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var primerResultado = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'search_result_row')]");
            if (primerResultado != null)
            {
                // Intentar obtener el app ID para hacer request a la página del juego
                var dataDs = primerResultado.GetAttributeValue("data-ds-appid", "");
                if (!string.IsNullOrEmpty(dataDs))
                {
                    var urlJuego = $"https://store.steampowered.com/app/{dataDs}";
                    var htmlJuego = await _httpClient.GetStringAsync(urlJuego);
                    var docJuego = new HtmlDocument();
                    docJuego.LoadHtml(htmlJuego);

                    // Buscar géneros en los tags
                    var generoNodes = docJuego.DocumentNode.SelectNodes("//div[@class='glance_tags']//a")
                                   ?? docJuego.DocumentNode.SelectNodes("//a[contains(@class, 'app_tag')]");

                    if (generoNodes != null && generoNodes.Any())
                    {
                        var generos = generoNodes
                            .Take(3)
                            .Select(n => n.InnerText.Trim())
                            .Where(g => !string.IsNullOrEmpty(g))
                            .ToList();

                        if (generos.Any())
                            return string.Join(", ", generos);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener género para {Titulo}: {Error}", titulo, ex.Message);
        }

        return "Acción, Aventura"; // Valor por defecto
    }

    public async Task<ScrapingResult> ObtenerOfertasAsync()
    {
        var inicio = DateTime.Now;
        var resultado = new ScrapingResult { Fuente = Nombre };

        try
        {
            _logger.LogInformation("🔍 Scrapeando Steam...");

            var juegos = new List<Videojuego>();
            var url = "https://store.steampowered.com/search/?specials=1&filter=popularwishlist";

            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var nodosJuegos = document.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");

            if (nodosJuegos != null)
            {
                _logger.LogInformation("🎯 Encontrados {Count} juegos en Steam", nodosJuegos.Count);

                foreach (var nodo in nodosJuegos.Take(25))
                {
                    try
                    {
                        var juego = await ProcesarJuegoSteam(nodo);
                        if (juego != null && juego.PrecioDescuento > 0)
                        {
                            juegos.Add(juego);
                            _logger.LogInformation("✅ {Titulo} - ${Precio}", juego.Titulo, juego.PrecioDescuento);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error procesando juego de Steam");
                    }
                }
            }

            resultado.Videojuegos = juegos;
            resultado.Success = true;
            _logger.LogInformation("🎉 Steam scraping completado: {Count} juegos", juegos.Count);
        }
        catch (Exception ex)
        {
            resultado.Success = false;
            resultado.Error = ex.Message;
            _logger.LogError(ex, "❌ Error en SteamScraper");
        }

        resultado.TiempoProcesamiento = DateTime.Now - inicio;
        return resultado;
    }



    private async Task<Videojuego> ProcesarJuegoSteam(HtmlNode nodo)
    {
        try
        {
            // Extraer título
            var titulo = nodo.SelectSingleNode(".//span[@class='title']")?.InnerText?.Trim();

            if (string.IsNullOrEmpty(titulo))
                return null;

            // Extraer precios - selectores más específicos
            var precioOriginalNode = nodo.SelectSingleNode(".//div[contains(@class, 'discount_original_price')]");
            var precioDescuentoNode = nodo.SelectSingleNode(".//div[contains(@class, 'discount_final_price')]");
            var descuentoNode = nodo.SelectSingleNode(".//div[contains(@class, 'discount_pct')]");

            // Debug: mostrar lo que se encontró
            _logger.LogDebug("Título: {Titulo}", titulo);
            _logger.LogDebug("Precio original: {PrecioOriginal}", precioOriginalNode?.InnerText);
            _logger.LogDebug("Precio descuento: {PrecioDescuento}", precioDescuentoNode?.InnerText);
            _logger.LogDebug("Descuento: {Descuento}", descuentoNode?.InnerText);

            var precioOriginal = ExtraerPrecio(precioOriginalNode?.InnerText);
            var precioDescuento = ExtraerPrecio(precioDescuentoNode?.InnerText);
            var descuento = ExtraerDescuento(descuentoNode?.InnerText);

            // Si no hay descuento, usar precio final como ambos
            if (precioOriginal == 0 && precioDescuento > 0)
            {
                precioOriginal = precioDescuento;
                descuento = 0;
            }

            // Extraer imagen
            var imagenNode = nodo.SelectSingleNode(".//img[contains(@class, 'game_capsule')]")
                           ?? nodo.SelectSingleNode(".//img");
            var urlImagen = imagenNode?.GetAttributeValue("src", "");

            // URL del juego
            var urlProducto = nodo.GetAttributeValue("href", "");

            // Datos adicionales
            var random = new Random();
            var calificacion = Math.Round((decimal)(random.NextDouble() * 2 + 3), 1);
            var totalResenas = random.Next(100, 5000);

            return new Videojuego
            {
                Titulo = titulo,
                Descripcion = $"Disfruta de {titulo} en Steam. {(descuento > 0 ? $"{descuento}% de descuento por tiempo limitado." : "Disponible ahora.")}",
                PrecioOriginal = precioOriginal,
                PrecioDescuento = precioDescuento,
                PorcentajeDescuento = descuento,
                Tipo = "Digital",  // ← AGREGAR ESTA LÍNEA
                Formato = await ObtenerGeneroReal(titulo), // ← AGREGAR ESTA LÍNEA
                Plataformas = new List<string> { "PC" },
                Calificacion = calificacion,
                TotalReseñas = totalResenas,
                TiempoCompletar = $"{random.Next(8, 60)} horas",
                UrlImagen = urlImagen,
                UrlProducto = urlProducto,
                PreciosTiendas = new List<TiendaPrecio>
    {
        new TiendaPrecio
        {
            NombreTienda = "Steam",
            Precio = precioDescuento,
            PrecioOriginal = precioOriginal,
            Descuento = descuento,
            UrlTienda = urlProducto
        }
    }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Error procesando juego de Steam");
            return null;
        }
    }

   

    private decimal ExtraerPrecio(string textoPrecio)
    {
        if (string.IsNullOrEmpty(textoPrecio)) return 0;

        // Limpiar el texto del precio - manejar múltiples formatos
        var textoLimpio = textoPrecio
            .Replace("$", "")
            .Replace("€", "")
            .Replace("USD", "")
            .Replace("EUR", "")
            .Replace(" ", "")
            .Replace("€", "")
            .Replace("¥", "")
            .Replace("£", "")
            .Replace("₩", "")
            .Replace("₹", "")
            .Trim();

        // Buscar números con decimales
        var match = System.Text.RegularExpressions.Regex.Match(textoLimpio, @"(\d+[.,]\d+)|(\d+)");
        if (match.Success)
        {
            var numero = match.Value.Replace(",", ".");
            return decimal.TryParse(numero, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var precio) ? precio : 0;
        }

        return 0;
    }

    private int ExtraerDescuento(string textoDescuento)
    {
        if (string.IsNullOrEmpty(textoDescuento)) return 0;

        var textoLimpio = textoDescuento
            .Replace("-", "")
            .Replace("%", "")
            .Replace(" ", "")
            .Replace("OFF", "")
            .Trim();

        return int.TryParse(textoLimpio, out var descuento) ? descuento : 0;
    }
}

// Scrapers/EpicGamesScraper.cs


// Scrapers/EpicGamesScraper.cs
public class EpicGamesScraper : IScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EpicGamesScraper> _logger;

    public string Nombre => "Epic Games";

    public EpicGamesScraper(HttpClient httpClient, ILogger<EpicGamesScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }
    private async Task<string> ObtenerGeneroDesdeApi(Element elemento)
    {
        try
        {
            if (elemento.categories != null && elemento.categories.Any())
            {
                return string.Join(", ", elemento.categories.Take(3).Select(c => c.path));
            }

            // Si no hay categorías, intentar desde tags
            if (elemento.tags != null && elemento.tags.Any())
            {
                return string.Join(", ", elemento.tags.Take(3).Select(t => t.name ?? t.id));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error obteniendo género desde API Epic: {Error}", ex.Message);
        }

        return null;
    }
    public async Task<ScrapingResult> ObtenerOfertasAsync()
    {
        var inicio = DateTime.Now;
        var resultado = new ScrapingResult { Fuente = Nombre };

        try
        {
            _logger.LogInformation("🔍 Scrapeando Epic Games...");

            var juegos = new List<Videojuego>();

            // Usar la API real de Epic Games para obtener juegos en oferta
            var url = "https://store-site-backend-static.ak.epicgames.com/freeGamesPromotions?locale=es-ES&country=US&allowCountries=US";

            var response = await _httpClient.GetStringAsync(url);
            var juegosEpic = System.Text.Json.JsonSerializer.Deserialize<EpicGamesResponse>(response);

            if (juegosEpic?.data?.Catalog?.searchStore?.elements != null)
            {
                foreach (var elemento in juegosEpic.data.Catalog.searchStore.elements)
                {
                    try
                    {
                        // Solo procesar juegos que están en oferta o tienen precio
                        if (elemento.title != null && elemento.price?.totalPrice != null)
                        {
                            var precioOriginal = elemento.price.totalPrice.originalPrice / 100m;
                            var precioDescuento = elemento.price.totalPrice.discountPrice > 0
                                ? elemento.price.totalPrice.discountPrice / 100m
                                : precioOriginal;

                            var descuento = precioOriginal > 0 && precioDescuento < precioOriginal
                                ? (int)((1 - precioDescuento / precioOriginal) * 100)
                                : 0;

                            // Solo incluir juegos que tienen descuento o son gratuitos
                            if (descuento > 0 || precioDescuento == 0)
                            {
                                var juego = new Videojuego
                                {
                                    Titulo = elemento.title,
                                    Descripcion = elemento.description ?? $"Disfruta de {elemento.title} en Epic Games Store.",
                                    PrecioOriginal = precioOriginal,
                                    PrecioDescuento = precioDescuento,
                                    PorcentajeDescuento = descuento,
                                    Tipo = "Digital",  // ← AGREGAR
                                    Formato = await ObtenerGeneroDesdeApi(elemento) ?? "Acción, Aventura", // ← AGREGAR
                                    Plataformas = new List<string> { "PC" },
                                    Calificacion = await ObtenerCalificacionReal(elemento.title),
                                    TotalReseñas = await ObtenerTotalResenasReal(elemento.title),
                                    TiempoCompletar = await ObtenerTiempoCompletarReal(elemento.title),
                                    UrlImagen = elemento.keyImages?.FirstOrDefault(k => k.type == "DieselStoreFrontWide")?.url
                                               ?? elemento.keyImages?.FirstOrDefault()?.url
                                               ?? "",
                                    UrlProducto = ObtenerUrlProductoEpic(elemento),
                                    PreciosTiendas = new List<TiendaPrecio>
                                    {
                                        new TiendaPrecio
                                        {
                                            NombreTienda = "Epic Games",
                                            Precio = precioDescuento,
                                            PrecioOriginal = precioOriginal,
                                            Descuento = descuento,
                                            UrlTienda = ObtenerUrlProductoEpic(elemento)
                                        }
                                    }
                                };

                                juegos.Add(juego);
                                _logger.LogInformation("✅ Epic: {Titulo} - ${Precio} (${Original}) {Descuento}%",
                                    juego.Titulo, juego.PrecioDescuento, juego.PrecioOriginal, juego.PorcentajeDescuento);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error procesando juego de Epic Games: {Titulo}", elemento.title);
                    }
                }
            }

            resultado.Videojuegos = juegos;
            resultado.Success = true;
            _logger.LogInformation("🎉 Epic Games scraping completado: {Count} juegos", juegos.Count);
        }
        catch (Exception ex)
        {
            resultado.Success = false;
            resultado.Error = ex.Message;
            _logger.LogError(ex, "❌ Error en EpicGamesScraper");
        }

        resultado.TiempoProcesamiento = DateTime.Now - inicio;
        return resultado;
    }



    private string ObtenerUrlProductoEpic(Element elemento)
    {
        if (!string.IsNullOrEmpty(elemento.productSlug))
            return $"https://store.epicgames.com/es-ES/p/{elemento.productSlug}";

        if (!string.IsNullOrEmpty(elemento.urlSlug))
            return $"https://store.epicgames.com/es-ES/browse?q={Uri.EscapeDataString(elemento.urlSlug)}";

        return $"https://store.epicgames.com/es-ES/search?q={Uri.EscapeDataString(elemento.title)}";
    }

    private async Task<decimal> ObtenerCalificacionReal(string titulo)
    {
        try
        {
            // Buscar calificación en Steam como referencia real
            var url = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(titulo)}";
            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var primerResultado = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'search_result_row')]");
            if (primerResultado != null)
            {
                var reviewNode = primerResultado.SelectSingleNode(".//span[contains(@class, 'search_review_summary')]");
                if (reviewNode != null)
                {
                    var dataTooltipHtml = reviewNode.GetAttributeValue("data-tooltip-html", "");
                    var match = System.Text.RegularExpressions.Regex.Match(dataTooltipHtml, @"(\d+)%");
                    if (match.Success)
                    {
                        var porcentaje = int.Parse(match.Groups[1].Value);
                        return Math.Round(porcentaje / 20.0m, 1); // Convertir porcentaje a escala 1-5
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener calificación real para {Titulo}: {Error}", titulo, ex.Message);
        }

        return 4.0m; // Valor por defecto si no se puede obtener
    }

    private async Task<int> ObtenerTotalResenasReal(string titulo)
    {
        try
        {
            var url = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(titulo)}";
            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var primerResultado = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'search_result_row')]");
            if (primerResultado != null)
            {
                var reviewNode = primerResultado.SelectSingleNode(".//span[contains(@class, 'search_review_summary')]");
                if (reviewNode != null)
                {
                    var dataTooltipHtml = reviewNode.GetAttributeValue("data-tooltip-html", "");
                    var match = System.Text.RegularExpressions.Regex.Match(dataTooltipHtml, @"(\d+,?\d*) reviews");
                    if (match.Success)
                    {
                        var numeroTexto = match.Groups[1].Value.Replace(",", "");
                        return int.Parse(numeroTexto);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener reseñas reales para {Titulo}: {Error}", titulo, ex.Message);
        }

        return new Random().Next(100, 5000); // Valor por defecto
    }

    public async Task<string> ObtenerTiempoCompletarReal(string titulo)
    {
        try
        {
            // HowLongToBeat ahora requiere una búsqueda API más específica
            var searchUrl = "https://howlongtobeat.com/api/search";

            var searchPayload = new
            {
                searchType = "games",
                searchTerms = new[] { titulo },
                searchPage = 1,
                size = 1
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(searchPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(searchUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonDocument.Parse(jsonResponse);

                if (result.RootElement.TryGetProperty("data", out var data) &&
                    data.GetArrayLength() > 0)
                {
                    var firstGame = data[0];

                    // Buscar el tiempo principal (comp_main)
                    if (firstGame.TryGetProperty("comp_main", out var compMain))
                    {
                        var seconds = compMain.GetInt32();
                        if (seconds > 0)
                        {
                            var hours = seconds / 3600.0;
                            return $"{hours:F1} horas";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener tiempo real para {Titulo}: {Error}", titulo, ex.Message);
        }

        return $"{new Random().Next(10, 60)} horas";
    }

    // Clases para deserializar la respuesta de Epic Games (se mantienen igual)
    private class EpicGamesResponse { public Data data { get; set; } }
    private class Data { public Catalog Catalog { get; set; } }
    private class Catalog { public SearchStore searchStore { get; set; } }
    private class SearchStore { public List<Element> elements { get; set; } }
    private class Element
    {
        public string title { get; set; }
        public string description { get; set; }
        public string productSlug { get; set; }
        public string urlSlug { get; set; }
        public Price price { get; set; }
        public List<KeyImage> keyImages { get; set; }
        public List<Category> categories { get; set; }  // ← AGREGAR
        public List<Tag> tags { get; set; }  // ← AGREGAR

    }
    private class Price { public TotalPrice totalPrice { get; set; } }
    private class TotalPrice
    {
        public int discountPrice { get; set; }
        public int originalPrice { get; set; }
    }
    private class KeyImage
    {
        public string type { get; set; }
        public string url { get; set; }
    }
    private class Category
    {
        public string path { get; set; }
    }

    private class Tag
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}

// Scrapers/GogScraper.cs
// Scrapers/GogScraper.cs
public class GogScraper : IScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GogScraper> _logger;

    public string Nombre => "GOG";

    public GogScraper(HttpClient httpClient, ILogger<GogScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    private async Task<string> ObtenerGeneroGOG(string urlProducto)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(urlProducto);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Buscar géneros en GOG
            var generoNodes = document.DocumentNode.SelectNodes("//a[contains(@class, 'details__link')]")
                           ?? document.DocumentNode.SelectNodes("//div[contains(@class, 'table__row')]//a");

            if (generoNodes != null)
            {
                var generos = generoNodes
                    .Select(n => n.InnerText.Trim())
                    .Where(g => !string.IsNullOrEmpty(g) && !g.Contains("http") && g.Length < 30)
                    .Take(3)
                    .ToList();

                if (generos.Any())
                    return string.Join(", ", generos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error obteniendo género GOG: {Error}", ex.Message);
        }

        return "Acción, Aventura";
    }

    public async Task<ScrapingResult> ObtenerOfertasAsync()
    {
        var inicio = DateTime.Now;
        var resultado = new ScrapingResult { Fuente = Nombre };

        try
        {
            _logger.LogInformation("🔍 Scrapeando GOG...");

            var juegos = new List<Videojuego>();

            // Scraping real de la página de ofertas de GOG
            var url = "https://www.gog.com/en/games?priceRange=0,50&discounted=true";
            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var nodosJuegos = document.DocumentNode.SelectNodes("//a[contains(@class, 'product-tile')]");

            if (nodosJuegos != null)
            {
                _logger.LogInformation("🎯 Encontrados {Count} juegos en GOG", nodosJuegos.Count);

                foreach (var nodo in nodosJuegos.Take(25))
                {
                    try
                    {
                        var juego = await ProcesarJuegoGOG(nodo);
                        if (juego != null && juego.PorcentajeDescuento > 0)
                        {
                            juegos.Add(juego);
                            _logger.LogInformation("✅ GOG: {Titulo} - ${Precio} (${Original}) {Descuento}%",
                                juego.Titulo, juego.PrecioDescuento, juego.PrecioOriginal, juego.PorcentajeDescuento);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error procesando juego de GOG");
                    }
                }
            }

            resultado.Videojuegos = juegos;
            resultado.Success = true;
            _logger.LogInformation("🎉 GOG scraping completado: {Count} juegos", juegos.Count);
        }
        catch (Exception ex)
        {
            resultado.Success = false;
            resultado.Error = ex.Message;
            _logger.LogError(ex, "❌ Error en GogScraper");
        }

        resultado.TiempoProcesamiento = DateTime.Now - inicio;
        return resultado;
    }

    private async Task<Videojuego> ProcesarJuegoGOG(HtmlNode nodo)
    {
        try
        {
            // Extraer título
            var tituloNode = nodo.SelectSingleNode(".//span[contains(@class, 'product-tile__title')]");
            var titulo = tituloNode?.InnerText?.Trim();

            if (string.IsNullOrEmpty(titulo))
                return null;

            // Extraer precios
            var precioFinalNode = nodo.SelectSingleNode(".//span[contains(@class, 'product-tile__price--final')]");
            var precioBaseNode = nodo.SelectSingleNode(".//span[contains(@class, 'product-tile__price--base')]");
            var descuentoNode = nodo.SelectSingleNode(".//span[contains(@class, 'product-tile__discount')]");

            var precioDescuento = ExtraerPrecioGOG(precioFinalNode?.InnerText);
            var precioOriginal = ExtraerPrecioGOG(precioBaseNode?.InnerText);
            var descuento = ExtraerDescuentoGOG(descuentoNode?.InnerText);

            // Si no hay precio original, usar el de descuento
            if (precioOriginal == 0 && precioDescuento > 0)
                precioOriginal = precioDescuento;

            // Extraer URL e imagen
            var urlRelativa = nodo.GetAttributeValue("href", "");
            var urlProducto = $"https://www.gog.com{urlRelativa}";

            var imagenNode = nodo.SelectSingleNode(".//img[contains(@class, 'product-tile__image')]");
            var urlImagen = imagenNode?.GetAttributeValue("src", "");

            // Obtener datos adicionales reales - PASAR el título como parámetro
            var (calificacion, totalResenas, tiempoCompletar) = await ObtenerDetallesAdicionalesGOG(urlProducto, titulo);

            return new Videojuego
            {
                Titulo = titulo,
                Descripcion = await ObtenerDescripcionGOG(urlProducto),
                PrecioOriginal = precioOriginal,
                PrecioDescuento = precioDescuento,
                PorcentajeDescuento = descuento,
                Tipo = "Digital",  // ← AGREGAR
                Formato = await ObtenerGeneroGOG(urlProducto), // ← AGREGAR
                Plataformas = new List<string> { "PC" },
                Calificacion = calificacion,
                TotalReseñas = totalResenas,
                TiempoCompletar = tiempoCompletar,
                UrlImagen = urlImagen,
                UrlProducto = urlProducto,
                PreciosTiendas = new List<TiendaPrecio>
                {
                    new TiendaPrecio
                    {
                        NombreTienda = "GOG",
                        Precio = precioDescuento,
                        PrecioOriginal = precioOriginal,
                        Descuento = descuento,
                        UrlTienda = urlProducto
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Error procesando juego de GOG");
            return null;
        }
    }

    private async Task<string> ObtenerDescripcionGOG(string urlProducto)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(urlProducto);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var descripcionNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'description')]");
            return descripcionNode?.InnerText?.Trim() ?? "Juego disponible en GOG.com - Sin DRM, garantía de devolución de 30 días.";
        }
        catch
        {
            return "Juego disponible en GOG.com - Sin DRM, garantía de devolución de 30 días.";
        }
    }

    private async Task<(decimal calificacion, int totalResenas, string tiempoCompletar)> ObtenerDetallesAdicionalesGOG(string urlProducto, string titulo)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(urlProducto);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Calificación
            var ratingNode = document.DocumentNode.SelectSingleNode("//span[contains(@class, 'product-ratings__average')]");
            var calificacionTexto = ratingNode?.InnerText?.Trim();
            decimal calificacion = 4.0m;
            if (!string.IsNullOrEmpty(calificacionTexto) && decimal.TryParse(calificacionTexto, out var calif))
                calificacion = calif;

            // Total de reseñas
            var reviewsNode = document.DocumentNode.SelectSingleNode("//span[contains(@class, 'product-ratings__count')]");
            var reviewsTexto = reviewsNode?.InnerText?.Replace("reviews", "").Replace("(", "").Replace(")", "").Trim();
            int totalResenas = new Random().Next(100, 5000);
            if (!string.IsNullOrEmpty(reviewsTexto) && int.TryParse(reviewsTexto, out var resenas))
                totalResenas = resenas;

            // Tiempo para completar - AHORA el título está disponible como parámetro
            var tiempoCompletar = await ObtenerTiempoCompletarReal(titulo);

            return (calificacion, totalResenas, tiempoCompletar);
        }
        catch
        {
            // Si hay error, obtener tiempo usando el título que ahora está disponible
            var tiempoCompletar = await ObtenerTiempoCompletarReal(titulo);
            return (4.0m, new Random().Next(100, 5000), tiempoCompletar);
        }
    }

    private decimal ExtraerPrecioGOG(string textoPrecio)
    {
        if (string.IsNullOrEmpty(textoPrecio)) return 0;

        var match = System.Text.RegularExpressions.Regex.Match(textoPrecio, @"[\$€]?(\d+[.,]\d+)|[\$€]?(\d+)");
        if (match.Success)
        {
            var numero = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            numero = numero.Replace(",", ".");
            return decimal.TryParse(numero, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var precio) ? precio : 0;
        }

        return 0;
    }

    private int ExtraerDescuentoGOG(string textoDescuento)
    {
        if (string.IsNullOrEmpty(textoDescuento)) return 0;

        var match = System.Text.RegularExpressions.Regex.Match(textoDescuento, @"(\d+)%");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    public async Task<string> ObtenerTiempoCompletarReal(string titulo)
    {
        try
        {
            // HowLongToBeat ahora requiere una búsqueda API más específica
            var searchUrl = "https://howlongtobeat.com/api/search";

            var searchPayload = new
            {
                searchType = "games",
                searchTerms = new[] { titulo },
                searchPage = 1,
                size = 1
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(searchPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(searchUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonDocument.Parse(jsonResponse);

                if (result.RootElement.TryGetProperty("data", out var data) &&
                    data.GetArrayLength() > 0)
                {
                    var firstGame = data[0];

                    // Buscar el tiempo principal (comp_main)
                    if (firstGame.TryGetProperty("comp_main", out var compMain))
                    {
                        var seconds = compMain.GetInt32();
                        if (seconds > 0)
                        {
                            var hours = seconds / 3600.0;
                            return $"{hours:F1} horas";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener tiempo real para {Titulo}: {Error}", titulo, ex.Message);
        }

        return $"{new Random().Next(10, 60)} horas";
    }
}


public class BusquedaScraper : IScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BusquedaScraper> _logger;
    private readonly JuegoDataService _juegoDataService;
    private readonly CurrencyService _currencyService;

    public string Nombre => "Búsqueda Universal";

    public BusquedaScraper(HttpClient httpClient, ILogger<BusquedaScraper> logger,
                          JuegoDataService juegoDataService, CurrencyService currencyService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _juegoDataService = juegoDataService;
        _currencyService = currencyService;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,es;q=0.8");
    }

    private async Task<string> ObtenerGeneroRealBusqueda(string titulo)
    {
        try
        {
            var url = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(titulo)}";
            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var primerResultado = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'search_result_row')]");
            if (primerResultado != null)
            {
                var dataDs = primerResultado.GetAttributeValue("data-ds-appid", "");
                if (!string.IsNullOrEmpty(dataDs))
                {
                    var urlJuego = $"https://store.steampowered.com/app/{dataDs}";
                    var htmlJuego = await _httpClient.GetStringAsync(urlJuego);
                    var docJuego = new HtmlDocument();
                    docJuego.LoadHtml(htmlJuego);

                    var generoNodes = docJuego.DocumentNode.SelectNodes("//div[@class='glance_tags']//a")
                                   ?? docJuego.DocumentNode.SelectNodes("//a[contains(@class, 'app_tag')]");

                    if (generoNodes != null && generoNodes.Any())
                    {
                        return string.Join(", ", generoNodes.Take(3).Select(n => n.InnerText.Trim()));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error obteniendo género: {Error}", ex.Message);
        }

        return "Acción, Aventura";
    }

    public async Task<ScrapingResult> ObtenerOfertasAsync()
    {
        var inicio = DateTime.Now;
        var resultado = new ScrapingResult { Fuente = Nombre };

        try
        {
            await _currencyService.GetExchangeRateAsync();
            _logger.LogInformation("🔍 Iniciando búsqueda universal mejorada...");

            var nombresJuegos = _juegoDataService.CargarNombresJuegos();
            var todosJuegos = new List<Videojuego>();

            var semaphore = new SemaphoreSlim(2);

            _logger.LogInformation("📊 Procesando {Total} juegos", nombresJuegos.Count);

            var tareas = nombresJuegos.Select(async nombreJuego =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var juego = await BuscarJuegoEnTodasTiendas(nombreJuego);
                    if (juego != null)
                    {
                        lock (todosJuegos)
                        {
                            todosJuegos.Add(juego);
                        }
                        var tiendas = string.Join(", ", juego.PreciosTiendas.Select(p => p.NombreTienda));
                        _logger.LogInformation("✅ Encontrado: {Juego} - ${Precio} en {Tiendas}",
                            juego.Titulo, juego.PrecioDescuento, tiendas);
                    }
                    return juego;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error buscando {Juego}", nombreJuego);
                    return null;
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tareas);

            resultado.Videojuegos = todosJuegos;
            resultado.Success = true;
            _logger.LogInformation("🎉 Búsqueda completada: {Count} juegos encontrados", todosJuegos.Count);
        }
        catch (Exception ex)
        {
            resultado.Success = false;
            resultado.Error = ex.Message;
            _logger.LogError(ex, "❌ Error en BusquedaScraper");
        }

        resultado.TiempoProcesamiento = DateTime.Now - inicio;
        return resultado;
    }

    private async Task<Videojuego> BuscarJuegoEnTodasTiendas(string nombreJuego)
    {
        try
        {
            _logger.LogDebug("🔎 Buscando: {Juego}", nombreJuego);

            var preciosTiendas = new List<TiendaPrecio>();

            var steamTask = BuscarEnSteamMejorado(nombreJuego);
            var epicTask = BuscarEnEpicGamesMejorado(nombreJuego);
            var gogTask = BuscarEnGOGMejorado(nombreJuego);

            await Task.WhenAll(steamTask, epicTask, gogTask);

            var precioSteam = await steamTask;
            var precioEpic = await epicTask;
            var precioGog = await gogTask;

            if (precioSteam != null) preciosTiendas.Add(precioSteam);
            if (precioEpic != null) preciosTiendas.Add(precioEpic);
            if (precioGog != null) preciosTiendas.Add(precioGog);

            if (!preciosTiendas.Any())
            {
                _logger.LogDebug("❌ {Juego} no encontrado en tiendas", nombreJuego);
                return null;
            }

            var detalles = await ObtenerDetallesCompletosJuego(nombreJuego, preciosTiendas);
            var mejorPrecio = preciosTiendas.OrderBy(p => p.Precio).First();

            return new Videojuego
            {
                Titulo = detalles.titulo,
                Descripcion = detalles.descripcion,
                PrecioOriginal = mejorPrecio.PrecioOriginal,
                PrecioDescuento = mejorPrecio.Precio,
                PorcentajeDescuento = mejorPrecio.Descuento,
                Tipo = "Digital",  // ← AGREGAR
                Formato = detalles.genero,  // ← AGREGAR
                Plataformas = detalles.plataformas,
                Calificacion = detalles.calificacion,
                TotalReseñas = detalles.totalResenas,
                TiempoCompletar = detalles.tiempoCompletar,
                UrlImagen = detalles.urlImagen,
                UrlProducto = mejorPrecio.UrlTienda,
                PreciosTiendas = preciosTiendas
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error buscando {Juego}", nombreJuego);
            return null;
        }
    }

    private async Task<TiendaPrecio> BuscarEnSteamMejorado(string nombreJuego)
    {
        try
        {
            var url = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(nombreJuego)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", "birthtime=283993201; lastagecheckage=1-0-1980; steamCountry=CR%7C57bb6db5c8c7c8c7c8c7c8c7");
            request.Headers.Add("Accept-Language", "es-ES,es;q=0.9,en;q=0.8");

            var response = await _httpClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var resultados = document.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");

            if (resultados == null)
            {
                _logger.LogDebug("No se encontraron resultados en Steam para: {Juego}", nombreJuego);
                return null;
            }

            foreach (var resultado in resultados.Take(5))
            {
                var titulo = resultado.SelectSingleNode(".//span[@class='title']")?.InnerText?.Trim();

                if (titulo != null && CoincideTituloMejorado(titulo, nombreJuego))
                {
                    _logger.LogDebug("🎯 Encontrado en Steam: {Titulo}", titulo);

                    var precioFinalNode = resultado.SelectSingleNode(".//div[contains(@class, 'discount_final_price')]");
                    var precioOriginalNode = resultado.SelectSingleNode(".//div[contains(@class, 'discount_original_price')]");
                    var precioNormalNode = resultado.SelectSingleNode(".//div[contains(@class, 'search_price')]");
                    var descuentoNode = resultado.SelectSingleNode(".//div[contains(@class, 'discount_pct')]");

                    var precioTexto = precioFinalNode?.InnerText?.Trim() ?? precioNormalNode?.InnerText?.Trim();
                    var precioOriginalTexto = precioOriginalNode?.InnerText?.Trim();

                    if (string.IsNullOrEmpty(precioTexto))
                        continue;

                    var moneda = _currencyService.DetectCurrency(precioTexto);
                    var (precio, precioOriginal) = ExtraerPreciosSteam(precioTexto, precioOriginalTexto, moneda);

                    var descuento = ExtraerDescuento(descuentoNode?.InnerText);

                    var urlJuego = resultado.GetAttributeValue("href", "");

                    if (urlJuego.Contains("?"))
                        urlJuego = urlJuego.Split('?')[0];

                    if (precio >= 0)
                    {
                        _logger.LogDebug("💰 Steam - {Titulo}: {Moneda} {Precio} -> USD {PrecioUSD}",
                            titulo, moneda, precio, _currencyService.ConvertToDollars(precio, moneda));

                        return new TiendaPrecio
                        {
                            NombreTienda = "Steam",
                            Precio = _currencyService.ConvertToDollars(precio, moneda),
                            PrecioOriginal = _currencyService.ConvertToDollars(precioOriginal, moneda),
                            Descuento = descuento,
                            UrlTienda = urlJuego
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error buscando en Steam: {Juego} - {Error}", nombreJuego, ex.Message);
        }

        return null;
    }

    private (decimal precio, decimal precioOriginal) ExtraerPreciosSteam(string precioTexto, string precioOriginalTexto, string moneda)
    {
        decimal precio = -1;
        decimal precioOriginal = -1;

        try
        {
            if (moneda == "CRC")
            {
                var match = System.Text.RegularExpressions.Regex.Match(precioTexto, @"₡\s*([\d.,]+)");
                if (!match.Success)
                    match = System.Text.RegularExpressions.Regex.Match(precioTexto, @"([\d.,]+)\s*colones");

                if (match.Success)
                {
                    var precioStr = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
                    precio = decimal.Parse(precioStr, System.Globalization.CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrEmpty(precioOriginalTexto))
                {
                    match = System.Text.RegularExpressions.Regex.Match(precioOriginalTexto, @"₡\s*([\d.,]+)");
                    if (!match.Success)
                        match = System.Text.RegularExpressions.Regex.Match(precioOriginalTexto, @"([\d.,]+)\s*colones");

                    if (match.Success)
                    {
                        var precioOriginalStr = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
                        precioOriginal = decimal.Parse(precioOriginalStr, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            else
            {
                precio = ExtraerPrecioSimple(precioTexto);
                precioOriginal = ExtraerPrecioSimple(precioOriginalTexto);
            }

            if (precioOriginal <= 0 && precio > 0)
                precioOriginal = precio;

        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error extrayendo precios Steam: {Error}", ex.Message);
        }

        return (precio, precioOriginal);
    }

    private async Task<TiendaPrecio> BuscarEnEpicGamesMejorado(string nombreJuego)
    {
        try
        {
            var url = $"https://store.epicgames.com/es-ES/browse?q={Uri.EscapeDataString(nombreJuego)}&sortBy=relevancy&sortDir=DESC&count=20";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept-Language", "es-ES,es;q=0.9");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var productLinks = document.DocumentNode.SelectNodes("//a[contains(@href, '/p/')]") ??
                                 document.DocumentNode.SelectNodes("//a[contains(@data-testid, 'product-card')]");

                if (productLinks != null)
                {
                    foreach (var link in productLinks.Take(10))
                    {
                        var tituloNode = link.SelectSingleNode(".//div[contains(@data-testid, 'title')]") ??
                                       link.SelectSingleNode(".//span[contains(@class, 'css-1xkh1dj')]") ??
                                       link.SelectSingleNode(".//div[contains(@class, 'css-1h2ruwl')]") ??
                                       link.SelectSingleNode(".//h3") ??
                                       link.SelectSingleNode(".//div[contains(@class, 'product-title')]");

                        var titulo = tituloNode?.InnerText?.Trim();

                        if (titulo != null && CoincideTituloMejorado(titulo, nombreJuego))
                        {
                            var precioNode = link.SelectSingleNode(".//span[contains(@data-testid, 'price')]") ??
                                           link.SelectSingleNode(".//div[contains(@class, 'css-4jky3p')]") ??
                                           link.SelectSingleNode(".//span[contains(@class, 'css-119zqif')]");

                            var precioTexto = precioNode?.InnerText?.Trim();
                            decimal precio = 0;

                            if (string.IsNullOrEmpty(precioTexto) ||
                                precioTexto.Contains("Gratis", StringComparison.OrdinalIgnoreCase) ||
                                precioTexto.Contains("Free", StringComparison.OrdinalIgnoreCase))
                            {
                                precio = 0;
                            }
                            else
                            {
                                precio = ExtraerPrecioSimple(precioTexto);
                            }

                            var urlRelativa = link.GetAttributeValue("href", "");
                            var urlCompleta = urlRelativa.StartsWith("http") ?
                                            urlRelativa :
                                            $"https://store.epicgames.com{urlRelativa}";

                            _logger.LogDebug("💰 Epic Games - {Titulo}: USD {Precio}", titulo, precio);

                            return new TiendaPrecio
                            {
                                NombreTienda = "Epic Games",
                                Precio = precio,
                                PrecioOriginal = precio,
                                Descuento = 0,
                                UrlTienda = urlCompleta
                            };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error buscando en Epic Games: {Juego} - {Error}", nombreJuego, ex.Message);
        }

        return null;
    }

    private async Task<TiendaPrecio> BuscarEnGOGMejorado(string nombreJuego)
    {
        try
        {
            var url = $"https://www.gog.com/en/games?search={Uri.EscapeDataString(nombreJuego)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var productos = document.DocumentNode.SelectNodes("//a[contains(@class, 'product-tile')]");

                if (productos != null)
                {
                    foreach (var producto in productos.Take(10))
                    {
                        var tituloNode = producto.SelectSingleNode(".//span[contains(@class, 'product-tile__title')]") ??
                                       producto.SelectSingleNode(".//div[contains(@class, 'product-tile__info')]//span");

                        var titulo = tituloNode?.InnerText?.Trim();

                        if (titulo != null && CoincideTituloMejorado(titulo, nombreJuego))
                        {
                            var precioFinalNode = producto.SelectSingleNode(".//span[contains(@class, 'product-tile__price--final')]");
                            var precioBaseNode = producto.SelectSingleNode(".//span[contains(@class, 'product-tile__price--base')]");
                            var descuentoNode = producto.SelectSingleNode(".//span[contains(@class, 'product-tile__discount')]");

                            var precioFinal = ExtraerPrecioSimple(precioFinalNode?.InnerText);
                            var precioBase = ExtraerPrecioSimple(precioBaseNode?.InnerText);
                            var descuento = ExtraerDescuento(descuentoNode?.InnerText);

                            if (precioFinal >= 0)
                            {
                                var urlRelativa = producto.GetAttributeValue("href", "");
                                var urlCompleta = urlRelativa.StartsWith("http") ?
                                                urlRelativa :
                                                $"https://www.gog.com{urlRelativa}";

                                _logger.LogDebug("💰 GOG - {Titulo}: USD {PrecioFinal} (Original: {PrecioBase})",
                                    titulo, precioFinal, precioBase);

                                return new TiendaPrecio
                                {
                                    NombreTienda = "GOG",
                                    Precio = precioFinal,
                                    PrecioOriginal = precioBase > 0 ? precioBase : precioFinal,
                                    Descuento = descuento,
                                    UrlTienda = urlCompleta
                                };
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error buscando en GOG: {Juego} - {Error}", nombreJuego, ex.Message);
        }

        return null;
    }

    private bool CoincideTituloMejorado(string tituloEncontrado, string tituloBuscado)
    {
        if (string.IsNullOrEmpty(tituloEncontrado) || string.IsNullOrEmpty(tituloBuscado))
            return false;

        var encontrado = tituloEncontrado.ToLower();
        var buscado = tituloBuscado.ToLower();

        if (encontrado == buscado)
            return true;

        if (encontrado.Contains(buscado))
            return true;

        var palabrasBuscadas = buscado.Split(new[] { ' ', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var palabrasEncontradas = encontrado.Split(new[] { ' ', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        if (palabrasBuscadas.Length == 0)
            return false;

        var palabrasCoincidentes = palabrasBuscadas.Count(palabra =>
            palabrasEncontradas.Any(pe => pe.Contains(palabra) || palabra.Contains(pe)));

        var porcentajeCoincidencia = (decimal)palabrasCoincidentes / palabrasBuscadas.Length;

        return porcentajeCoincidencia >= 0.5m;
    }

    private decimal ExtraerPrecioSimple(string textoPrecio)
    {
        if (string.IsNullOrEmpty(textoPrecio)) return -1;

        if (textoPrecio.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
            textoPrecio.Contains("Gratis", StringComparison.OrdinalIgnoreCase) ||
            textoPrecio.Contains("Play for Free!", StringComparison.OrdinalIgnoreCase))
            return 0;

        var match = System.Text.RegularExpressions.Regex.Match(textoPrecio, @"[\$€₡£]?\s*(\d+[.,]\d+)|\$?\s*(\d+)");
        if (match.Success)
        {
            var numeroTexto = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            numeroTexto = numeroTexto.Replace(",", ".");
            return decimal.TryParse(numeroTexto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var precio) ? precio : -1;
        }

        return -1;
    }

    private int ExtraerDescuento(string textoDescuento)
    {
        if (string.IsNullOrEmpty(textoDescuento)) return 0;

        var match = System.Text.RegularExpressions.Regex.Match(textoDescuento, @"(\d+)%");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private async Task<(string titulo, string descripcion, string genero, List<string> plataformas, decimal calificacion, int totalResenas, string tiempoCompletar, string urlImagen)>
        ObtenerDetallesCompletosJuego(string nombreJuego, List<TiendaPrecio> preciosTiendas)
    {
        try
        {
            var url = preciosTiendas.FirstOrDefault(p => p.NombreTienda == "Steam")?.UrlTienda
                     ?? preciosTiendas.First().UrlTienda;

            var html = await _httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var tituloReal = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "")
                           ?? document.DocumentNode.SelectSingleNode("//title")?.InnerText
                           ?? nombreJuego;

            tituloReal = System.Text.RegularExpressions.Regex.Replace(tituloReal, @"\s*on\s+Steam|\s*-\s*Epic\s+Games\s*Store|\s*-\s*GOG\.com", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            var descripcion = document.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", "")
                            ?? document.DocumentNode.SelectSingleNode("//meta[name='description']")?.GetAttributeValue("content", "")
                            ?? $"Disfruta de {tituloReal} en tu plataforma favorita.";

            descripcion = System.Text.RegularExpressions.Regex.Replace(descripcion, @"https?://\S+", "").Trim();
            if (descripcion.Length > 200)
                descripcion = descripcion.Substring(0, 200) + "...";

            var urlImagen = document.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", "")
                          ?? GenerarUrlImagen(nombreJuego);

            var plataformas = await ObtenerPlataformasReales(nombreJuego);
            var (calificacion, totalResenas) = await ObtenerCalificacionReal(nombreJuego);
            var tiempoCompletar = await ObtenerTiempoCompletarReal(nombreJuego);
            var genero = await ObtenerGeneroRealBusqueda(nombreJuego);

            return (tituloReal, descripcion, genero, plataformas, calificacion, totalResenas, tiempoCompletar, urlImagen);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error obteniendo detalles reales para {Juego}: {Error}", nombreJuego, ex.Message);

            return (nombreJuego,
                    $"Disfruta de {nombreJuego} en tu plataforma favorita.",
                    "Acción, Aventura", // ← AGREGAR
                    new List<string> { "PC" },
                    4.0m,
                    new Random().Next(100, 5000),
                    "No disponible",
                    GenerarUrlImagen(nombreJuego));
        }
    }

    private async Task<List<string>> ObtenerPlataformasReales(string nombreJuego)
    {
        try
        {
            var urlSteam = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(nombreJuego)}";
            var html = await _httpClient.GetStringAsync(urlSteam);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var primerResultado = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'search_result_row')]");
            if (primerResultado != null)
            {
                var plataformas = new List<string> { "PC" };

                var windowsNode = primerResultado.SelectSingleNode(".//span[contains(@class, 'platform_img win')]");
                var macNode = primerResultado.SelectSingleNode(".//span[contains(@class, 'platform_img mac')]");
                var linuxNode = primerResultado.SelectSingleNode(".//span[contains(@class, 'platform_img linux')]");

                if (windowsNode != null) plataformas.Add("Windows");
                if (macNode != null) plataformas.Add("Mac");
                if (linuxNode != null) plataformas.Add("Linux");

                return plataformas.Distinct().ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudieron obtener plataformas reales para {Juego}: {Error}", nombreJuego, ex.Message);
        }

        return new List<string> { "PC" };
    }

    private async Task<(decimal calificacion, int totalResenas)> ObtenerCalificacionReal(string nombreJuego)
    {
        try
        {
            var urlSteam = $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(nombreJuego)}";
            var html = await _httpClient.GetStringAsync(urlSteam);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var primerResultado = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'search_result_row')]");
            if (primerResultado != null)
            {
                var reviewNode = primerResultado.SelectSingleNode(".//span[contains(@class, 'search_review_summary')]");
                if (reviewNode != null)
                {
                    var dataTooltipHtml = reviewNode.GetAttributeValue("data-tooltip-html", "");

                    var porcentajeMatch = System.Text.RegularExpressions.Regex.Match(dataTooltipHtml, @"(\d+)%");
                    var reseñasMatch = System.Text.RegularExpressions.Regex.Match(dataTooltipHtml, @"(\d+,?\d*)\s*user reviews");

                    decimal calificacion = 0;
                    int totalResenas = 0;

                    if (porcentajeMatch.Success)
                    {
                        var porcentaje = int.Parse(porcentajeMatch.Groups[1].Value);
                        calificacion = Math.Round(porcentaje / 20.0m, 1);
                    }

                    if (reseñasMatch.Success)
                    {
                        var numeroTexto = reseñasMatch.Groups[1].Value.Replace(",", "");
                        totalResenas = int.Parse(numeroTexto);
                    }

                    if (calificacion > 0 && totalResenas > 0)
                    {
                        return (calificacion, totalResenas);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener calificación real para {Juego}: {Error}", nombreJuego, ex.Message);
        }

        return (4.0m, new Random().Next(100, 5000));
    }

    public async Task<string> ObtenerTiempoCompletarReal(string titulo)
    {
        try
        {
            // HowLongToBeat ahora requiere una búsqueda API más específica
            var searchUrl = "https://howlongtobeat.com/api/search";

            var searchPayload = new
            {
                searchType = "games",
                searchTerms = new[] { titulo },
                searchPage = 1,
                size = 1
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(searchPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(searchUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonDocument.Parse(jsonResponse);

                if (result.RootElement.TryGetProperty("data", out var data) &&
                    data.GetArrayLength() > 0)
                {
                    var firstGame = data[0];

                    // Buscar el tiempo principal (comp_main)
                    if (firstGame.TryGetProperty("comp_main", out var compMain))
                    {
                        var seconds = compMain.GetInt32();
                        if (seconds > 0)
                        {
                            var hours = seconds / 3600.0;
                            return $"{hours:F1} horas";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No se pudo obtener tiempo real para {Titulo}: {Error}", titulo, ex.Message);
        }

        return $"{new Random().Next(10, 60)} horas";
    }

    private string GenerarUrlImagen(string nombreJuego)
    {
        return $"https://via.placeholder.com/800x400/1e293b/ffffff?text={Uri.EscapeDataString(nombreJuego)}";
    }

}



// Al final de IScraper.cs, antes de la última llave de cierre
public class CurrencyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyService> _logger;
    private decimal _exchangeRate = 535m;

    public CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<decimal> GetExchangeRateAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.exchangerate-api.com/v4/latest/USD");
            var exchangeData = System.Text.Json.JsonSerializer.Deserialize<ExchangeRateData>(response);
            _exchangeRate = exchangeData?.rates?.CRC ?? 535m;
            _logger.LogInformation("💰 Tasa de cambio actualizada: 1 USD = {Rate} CRC", _exchangeRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo obtener tasa de cambio, usando valor por defecto: {Error}", ex.Message);
        }
        return _exchangeRate;
    }

    public decimal ConvertToDollars(decimal priceInColones, string currencySymbol)
    {
        if (currencySymbol == "₡" || currencySymbol == "CRC")
        {
            return Math.Round(priceInColones / _exchangeRate, 2);
        }
        return priceInColones;
    }

    public string DetectCurrency(string priceText)
    {
        if (string.IsNullOrEmpty(priceText)) return "USD";

        if (priceText.Contains("₡") || priceText.Contains("CRC") || priceText.Contains("colones"))
            return "CRC";
        if (priceText.Contains("€"))
            return "EUR";
        if (priceText.Contains("£"))
            return "GBP";
        if (priceText.Contains("¥"))
            return "JPY";

        return "USD";
    }
}

public class ExchangeRateData
{
    public string base_code { get; set; }
    public Rates rates { get; set; }
}

public class Rates
{
    public decimal CRC { get; set; }
}


