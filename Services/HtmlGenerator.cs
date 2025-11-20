using OpenQA.Selenium.DevTools;
using System.Reflection.Metadata;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebScraperVideojuegos.Services;

using OpenQA.Selenium.DevTools;
using System.Text;
using WebScraperVideojuegos.Models;

public class HtmlGenerator
{
    public string GenerarPaginaWeb(List<Videojuego> juegos)
    {
        var htmlBuilder = new System.Text.StringBuilder();

        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html lang='es'>");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("    <meta charset='UTF-8'>");
        htmlBuilder.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        htmlBuilder.AppendLine("    <title>🎮 Comparador de Precios de Videojuegos</title>");
        htmlBuilder.AppendLine("    <style>");
        htmlBuilder.AppendLine(GenerarEstilos());
        htmlBuilder.AppendLine("    </style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");
        htmlBuilder.AppendLine("    <div class='container'>");
        htmlBuilder.AppendLine("        <header>");
        htmlBuilder.AppendLine("            <h1>🎮 Comparador de Precios de Videojuegos</h1>");
        htmlBuilder.AppendLine($"        <p class='subtitle'>Actualizado: {DateTime.Now:dd/MM/yyyy HH:mm} | <strong>{juegos.Count} juegos encontrados</strong></p>");
        htmlBuilder.AppendLine("        </header>");
        htmlBuilder.AppendLine(GenerarFiltros());

        // Estadísticas rápidas
        htmlBuilder.AppendLine(GenerarEstadisticas(juegos));

        htmlBuilder.AppendLine("        <div class='games-grid'>");

        foreach (var juego in juegos.OrderByDescending(j => j.PorcentajeDescuento))
        {
            htmlBuilder.AppendLine(GenerarTarjetaJuego(juego));
        }

        htmlBuilder.AppendLine("        </div>");
        htmlBuilder.AppendLine("    </div>");

        // Modal para detalles del juego
        htmlBuilder.AppendLine(GenerarModal());

        htmlBuilder.AppendLine("    <script>");
        htmlBuilder.AppendLine(GenerarJavaScript());
        htmlBuilder.AppendLine("    </script>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return htmlBuilder.ToString();
    }

    private string GenerarEstadisticas(List<Videojuego> juegos)
    {
        var juegosConDescuento = juegos.Count(j => j.PorcentajeDescuento > 0);
        var mejorDescuento = juegos.Any() ? juegos.Max(j => j.PorcentajeDescuento) : 0;
        var precioPromedio = juegos.Any() ? juegos.Average(j => j.PrecioDescuento) : 0;

        return $@"
        <div class='stats-container'>
            <div class='stat-card'>
                <div class='stat-number'>{juegos.Count}</div>
                <div class='stat-label'>Total Juegos</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{juegosConDescuento}</div>
                <div class='stat-label'>En Oferta</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>-{mejorDescuento}%</div>
                <div class='stat-label'>Mejor Descuento</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>${precioPromedio:F2}</div>
                <div class='stat-label'>Precio Promedio</div>
            </div>
        </div>";
    }

    private string GenerarFiltros()
    {
        return @"
    <div class='filters-container'>
        <div class='filter-group'>
            <label>Tipo:</label>
            <select id='filterTipo' onchange='aplicarFiltros()'>
                <option value=''>Todos</option>
                <option value='Digital'>Digital</option>
                <option value='Físico'>Físico</option>
            </select>
        </div>
        
        <div class='filter-group'>
            <label>Plataforma:</label>
            <select id='filterPlataforma' onchange='aplicarFiltros()'>
                <option value=''>Todas</option>
            </select>
        </div>
        
        <div class='filter-group'>
            <label>Género:</label>
            <select id='filterGenero' onchange='aplicarFiltros()'>
                <option value=''>Todos</option>
            </select>
        </div>
        
        <div class='filter-group'>
            <label>Tienda:</label>
            <select id='filterTienda' onchange='aplicarFiltros()'>
                <option value=''>Todas</option>
            </select>
        </div>
        
        <div class='filter-group'>
            <label>Ordenar por:</label>
            <select id='sortBy' onchange='aplicarFiltros()'>
                <option value='descuento'>Descuento (Mayor a Menor)</option>
                <option value='nombre'>Nombre (A-Z)</option>
                <option value='precio-asc'>Precio (Menor a Mayor)</option>
                <option value='precio-desc'>Precio (Mayor a Menor)</option>
                <option value='metacritic'>Score Metacritic</option>
            </select>
        </div>
        
        <button onclick='limpiarFiltros()' class='btn-clear'>Limpiar Filtros</button>
    </div>";
    }

    private string GenerarEstilos()
    {
        return @"
        * { 
            margin: 0; 
            padding: 0; 
            box-sizing: border-box; 
        }
        
        body { 
            font-family: 'Segoe UI', Arial, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333; 
            min-height: 100vh;
        }
        
        .container { 
            max-width: 1400px; 
            margin: 0 auto; 
            padding: 20px; 
        }
        
        header { 
            text-align: center; 
            margin-bottom: 30px; 
            background: white;
            padding: 30px;
            border-radius: 15px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.1);
        }
        
        header h1 { 
            color: #2c3e50; 
            margin-bottom: 10px;
            font-size: 2.5em;
        }
        
        .subtitle {
            color: #7f8c8d;
            font-size: 1.1em;
        }
        
        .stats-container {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-bottom: 30px;
        }
        
        .stat-card {
            background: white;
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
            transition: transform 0.3s;
        }
        
        .stat-card:hover {
            transform: translateY(-5px);
        }
        
        .stat-number {
            font-size: 2em;
            font-weight: bold;
            color: #3498db;
            margin-bottom: 5px;
        }
        
        .stat-label {
            color: #7f8c8d;
            font-size: 0.9em;
        }
        
        .games-grid { 
            display: grid; 
            grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); 
            gap: 25px; 
        }
        
        .game-card { 
            background: white; 
            border-radius: 15px; 
            box-shadow: 0 8px 25px rgba(0,0,0,0.1); 
            overflow: hidden; 
            transition: all 0.3s ease;
            cursor: pointer;
            position: relative;
        }
        
        .game-card:hover { 
            transform: translateY(-10px) scale(1.02);
            box-shadow: 0 15px 40px rgba(0,0,0,0.2);
        }
        
        .game-image-container {
            width: 100%;
            height: 180px;
            overflow: hidden;
            position: relative;
        }
        
        .game-image { 
            width: 100%; 
            height: 100%; 
            object-fit: cover;
            transition: transform 0.3s ease;
        }
        
        .game-card:hover .game-image {
            transform: scale(1.1);
        }
        
        .game-badge {
            position: absolute;
            top: 10px;
            right: 10px;
            background: #e74c3c;
            color: white;
            padding: 5px 10px;
            border-radius: 20px;
            font-size: 0.8em;
            font-weight: bold;
        }
        
        .game-content { 
            padding: 20px; 
        }
        
        .game-title { 
            font-size: 1.3em; 
            font-weight: bold; 
            margin-bottom: 12px; 
            color: #2c3e50;
            line-height: 1.3;
        }
        
        .price-section { 
            display: flex; 
            justify-content: space-between; 
            align-items: center; 
            margin-bottom: 12px; 
        }
        
        .original-price { 
            text-decoration: line-through; 
            color: #7f8c8d; 
            font-size: 0.9em; 
        }
        
        .discount-price { 
            font-size: 1.4em; 
            font-weight: bold; 
            color: #e74c3c; 
        }
        
        .discount-badge { 
            background: #e74c3c; 
            color: white; 
            padding: 4px 12px; 
            border-radius: 15px; 
            font-size: 0.8em; 
            font-weight: bold;
        }
        
        .platforms { 
            display: flex; 
            flex-wrap: wrap; 
            gap: 5px; 
            margin-bottom: 12px; 
        }
        
        .platform { 
            background: #3498db; 
            color: white; 
            padding: 3px 10px; 
            border-radius: 12px; 
            font-size: 0.7em; 
        }
        
        .stores { 
            font-size: 0.8em; 
            color: #7f8c8d; 
            margin-bottom: 8px;
        }
        
        .rating { 
            color: #f39c12; 
            margin-top: 8px; 
            font-size: 0.9em;
        }
        
        /* Modal Styles */
        .modal {
            display: none;
            position: fixed;
            z-index: 1000;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0,0,0,0.8);
            backdrop-filter: blur(5px);
        }
        
        .modal-content {
            background-color: white;
            margin: 5% auto;
            padding: 0;
            border-radius: 20px;
            width: 90%;
            max-width: 800px;
            max-height: 90vh;
            overflow-y: auto;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            animation: modalSlideIn 0.3s ease-out;
        }
        
        @keyframes modalSlideIn {
            from { transform: translateY(-50px); opacity: 0; }
            to { transform: translateY(0); opacity: 1; }
        }
        
        .modal-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 20px 20px 0 0;
            position: relative;
        }
        
        .close-modal {
            position: absolute;
            top: 20px;
            right: 25px;
            color: white;
            font-size: 2em;
            font-weight: bold;
            cursor: pointer;
            transition: transform 0.3s;
        }
        
        .close-modal:hover {
            transform: scale(1.2);
        }
        
        .modal-body {
            padding: 30px;
        }
        
        .modal-game-image {
            width: 100%;
            max-height: 300px;
            object-fit: cover;
            border-radius: 10px;
            margin-bottom: 20px;
        }
        
        .modal-price-section {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 10px;
            margin: 20px 0;
        }
        
        .store-price {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 0;
            border-bottom: 1px solid #e9ecef;
        }
        
        .store-price:last-child {
            border-bottom: none;
        }
        
        .best-price {
            background: #d4edda;
            border-left: 4px solid #28a745;
            padding-left: 15px;
        }
        
        .btn-buy {
            background: #28a745;
            color: white;
            padding: 10px 20px;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            text-decoration: none;
            display: inline-block;
            transition: background 0.3s;
        }
        
        .btn-buy:hover {
            background: #218838;
        }
        
        @media (max-width: 768px) {
            .games-grid {
                grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            }
            
            .stats-container {
                grid-template-columns: repeat(2, 1fr);
            }
            
            header h1 {
                font-size: 2em;
            }
        }

        .filters-container {
            background: white;
            padding: 25px;
            border-radius: 15px;
            box-shadow: 0 5px 20px rgba(0,0,0,0.1);
            margin-bottom: 30px;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 15px;
            align-items: end;
        }

        .filter-group {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .filter-group label {
            font-weight: 600;
            color: #2c3e50;
            font-size: 0.9em;
        }

        .filter-group select {
            padding: 10px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 0.95em;
            background: white;
            cursor: pointer;
            transition: border-color 0.3s;
        }

        .filter-group select:hover {
            border-color: #667eea;
        }

        .filter-group select:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }

        .btn-clear {
            padding: 10px 20px;
            background: #e74c3c;
            color: white;
            border: none;
            border-radius: 8px;
            cursor: pointer;
            font-weight: 600;
            transition: all 0.3s;
            align-self: end;
        }

        .btn-clear:hover {
            background: #c0392b;
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(231, 76, 60, 0.3);
        }

        @media (max-width: 768px) {
            .filters-container {
                grid-template-columns: 1fr;
            }
        }
        .game-link {
            text-decoration: none;
            color: inherit;
        }
        ";
    }

    private string GenerarTarjetaJuego(Videojuego juego)
    {
        var sb = new System.Text.StringBuilder();
        var mejorPrecio = juego.PreciosTiendas.OrderBy(p => p.Precio).FirstOrDefault();

        // Si no hay precios válidos, no mostrar el juego
        if (mejorPrecio == null || string.IsNullOrEmpty(mejorPrecio.NombreTienda))
            return string.Empty;

        sb.AppendLine($"<a href='juegos/{juego.Id}.html' class='game-link'>");

        sb.AppendLine(
            $"<div class='game-card' " +
            $"onclick=\"window.location.href='juegos/{juego.Id}.html'\" " +
            $"data-tipo='{juego.Tipo ?? "Digital"}' " +
            $"data-plataformas='{string.Join(",", juego.Plataformas)}' " +
            $"data-genero='{juego.Formato ?? "Acción, Aventura"}' " +
                    $"data-tiendas='{string.Join(",", juego.PreciosTiendas.Select(t => t.NombreTienda))}' " +
            $"data-precio='{mejorPrecio.Precio}' " +
            $"data-descuento='{juego.PorcentajeDescuento}' " +
            $"data-nombre='{juego.Titulo}' " +
            $"data-metacritic='{juego.Calificacion}'>");

        sb.AppendLine("<div class='game-image-container'>");
        if (!string.IsNullOrEmpty(juego.UrlImagen) && !juego.UrlImagen.Contains("example.com"))
        {
            sb.AppendLine($"<img src='{juego.UrlImagen}' alt='{juego.Titulo}' class='game-image' onerror=\"this.src='https://via.placeholder.com/400x200/667eea/white?text=Imagen+No+Disponible'\">");
        }
        else
        {
            sb.AppendLine($"<img src='https://via.placeholder.com/400x200/667eea/white?text={Uri.EscapeDataString(juego.Titulo)}' alt='{juego.Titulo}' class='game-image'>");
        }

        // Mostrar badge de descuento solo si hay descuento real
        if (juego.PorcentajeDescuento > 0)
        {
            sb.AppendLine($"<div class='game-badge'>-{juego.PorcentajeDescuento}%</div>");
        }
        // Mostrar badge "Gratis" si el precio es 0
        else if (mejorPrecio.Precio == 0)
        {
            sb.AppendLine($"<div class='game-badge' style='background: #28a745;'>GRATIS</div>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='game-content'>");
        sb.AppendLine($"<div class='game-title'>{juego.Titulo}</div>");

        // Precios - manejar precio $0.00
        sb.AppendLine("<div class='price-section'>");
        if (juego.PrecioOriginal > 0 && juego.PrecioOriginal != mejorPrecio.Precio)
        {
            sb.AppendLine($"<span class='original-price'>${juego.PrecioOriginal:F2}</span>");
        }

        // Mostrar "Gratis" en lugar de "$0.00"
        if (mejorPrecio.Precio == 0)
        {
            sb.AppendLine($"<span class='discount-price' style='color: #28a745;'>Gratis</span>");
        }
        else
        {
            sb.AppendLine($"<span class='discount-price'>${mejorPrecio.Precio:F2}</span>");
        }

        if (juego.PorcentajeDescuento > 0)
        {
            sb.AppendLine($"<span class='discount-badge'>-{juego.PorcentajeDescuento}%</span>");
        }
        sb.AppendLine("</div>");

        // Plataformas - solo mostrar si hay plataformas válidas
        var plataformasValidas = juego.Plataformas.Where(p => !string.IsNullOrEmpty(p)).ToList();
        if (plataformasValidas.Any())
        {
            sb.AppendLine("<div class='platforms'>");
            foreach (var plataforma in plataformasValidas.Take(3))
            {
                sb.AppendLine($"<span class='platform'>{plataforma}</span>");
            }
            sb.AppendLine("</div>");
        }

        // Tiendas - solo mostrar tiendas válidas
        var tiendasValidas = juego.PreciosTiendas
            .Where(t => !string.IsNullOrEmpty(t.NombreTienda))
            .Select(t => t.NombreTienda)
            .Distinct()
            .ToList();

        if (tiendasValidas.Any())
        {
            sb.AppendLine($"<div class='stores'>Disponible en: {string.Join(", ", tiendasValidas)}</div>");
        }

        // Rating - solo mostrar si hay calificación válida
        if (juego.Calificacion > 0 && juego.TotalReseñas > 0)
        {
            sb.AppendLine($"<div class='rating'>⭐ {juego.Calificacion}/5 ({juego.TotalReseñas} reseñas)</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</a>");

        return sb.ToString();
    }

    public string GenerarPaginaJuego(Videojuego juego)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine($"<title>{juego.Titulo}</title>");
        sb.AppendLine("<link href='../styles.css' rel='stylesheet'>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class='detalle-container'>");

        // Título
        sb.AppendLine($"<h1 class='detalle-titulo'>{juego.Titulo}</h1>");

        // Imagen
        if (!string.IsNullOrEmpty(juego.UrlImagen) && !juego.UrlImagen.Contains("example.com"))
        {
            sb.AppendLine($"<img src='{juego.UrlImagen}' class='detalle-imagen' alt='{juego.Titulo}'>");
        }
        else
        {
            sb.AppendLine($"<img src='https://via.placeholder.com/800x400/667eea/white?text={Uri.EscapeDataString(juego.Titulo)}' class='detalle-imagen' alt='{juego.Titulo}'>");
        }

        sb.AppendLine("<div class='detalle-info'>");

        // Información básica
        sb.AppendLine("<div style='margin-bottom: 25px;'>");
        sb.AppendLine($"<p><strong>Descripción:</strong> {juego.Descripcion}</p>");
        sb.AppendLine("</div>");

        // Detalles del juego en grid
        sb.AppendLine("<div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; margin-bottom: 30px;'>");

        // Tipo y Género
        sb.AppendLine($"<div><strong>Tipo:</strong><br>{juego.Tipo ?? "Digital"}</div>");
        sb.AppendLine($"<div><strong>Género:</strong><br>{juego.Formato ?? "No especificado"}</div>");

        // Precios
        sb.AppendLine($"<div><strong>Precio Original:</strong><br>{(juego.PrecioOriginal > 0 ? "$" + juego.PrecioOriginal.ToString("F2") : "No disponible")}</div>");
        sb.AppendLine($"<div><strong>Precio con Descuento:</strong><br>{(juego.PrecioDescuento > 0 ? "$" + juego.PrecioDescuento.ToString("F2") : "No disponible")}</div>");

        // Descuento
        sb.AppendLine($"<div><strong>Descuento:</strong><br>{(juego.PorcentajeDescuento > 0 ? juego.PorcentajeDescuento + "%" : "Sin descuento")}</div>");

        // Calificación y reseñas
        sb.AppendLine($"<div><strong>Calificación:</strong><br>⭐ {juego.Calificacion:F1}/5</div>");
        sb.AppendLine($"<div><strong>Total Reseñas:</strong><br>{juego.TotalReseñas}</div>");

        // Tiempo para completar
        sb.AppendLine($"<div><strong>Tiempo para Completar:</strong><br>{juego.TiempoCompletar ?? "No disponible"}</div>");

        // Fecha de actualización
        sb.AppendLine($"<div><strong>Última Actualización:</strong><br>{juego.FechaActualizacion:dd/MM/yyyy HH:mm}</div>");
        sb.AppendLine("</div>");

        // Plataformas
        sb.AppendLine("<div style='margin-bottom: 25px;'>");
        sb.AppendLine("<h3 style='color: #2c3e50; margin-bottom: 10px;'>Plataformas Disponibles</h3>");
        sb.AppendLine("<div class='platforms'>");
        foreach (var plataforma in juego.Plataformas)
        {
            sb.AppendLine($"<span class='platform'>{plataforma}</span>");
        }
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Lista de tiendas ordenadas por precio
        sb.AppendLine("<div>");
        sb.AppendLine("<h3 style='color: #2c3e50; margin-bottom: 15px;'>Precios en Tiendas</h3>");

        var preciosOrdenados = juego.PreciosTiendas
            .OrderBy(p => p.Precio)
            .ToList();

        sb.AppendLine("<ul class='lista-precios'>");

        foreach (var tienda in preciosOrdenados)
        {
            var esMejorPrecio = tienda == preciosOrdenados.First();
            var mostrarDescuento = tienda.Descuento > 0;
            var mostrarPrecioOriginal = tienda.PrecioOriginal > 0 && tienda.PrecioOriginal != tienda.Precio;

            sb.AppendLine($@"
            <li class='precio-item' {(esMejorPrecio ? "style='border-left: 5px solid #28a745; background: #f0fff4;'" : "")}>
                <div class='precio-textos'>
                    <strong>{tienda.NombreTienda}</strong>
                    {(mostrarDescuento ? $"<span style='color: #e74c3c; font-size: 0.9em; margin-left: 10px;'>-{tienda.Descuento}%</span>" : "")}
                    {(esMejorPrecio ? "<span style='color: #28a745; font-size: 0.9em; margin-left: 10px;'>🔥 MEJOR PRECIO</span>" : "")}
                </div>
                <div style='display: flex; align-items: center; gap: 10px;'>
                    {(mostrarPrecioOriginal ? $"<span class='precio-original'>${tienda.PrecioOriginal:F2}</span>" : "")}
                    <span class='precio'>${tienda.Precio:F2}</span>
                    <a href='{tienda.UrlTienda}' target='_blank' class='btn-buy'>Comprar</a>
                </div>
            </li>");
        }

        sb.AppendLine("</ul>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>"); // cierre de detalle-info

        // Botón volver
        sb.AppendLine("<a href='../index.html' class='volver'>← Volver al Listado Principal</a>");

        sb.AppendLine("</div>"); // cierre detalle-container
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }



    private string GenerarModal()
    {
        return @"
        <div id='gameModal' class='modal'>
            <div class='modal-content'>
                <div class='modal-header'>
                    <span class='close-modal' onclick='cerrarModal()'>&times;</span>
                    <h2 id='modalTitle'>Título del Juego</h2>
                </div>
                <div class='modal-body'>
                    <img id='modalImage' src='' alt='' class='modal-game-image'>
                    <div id='modalDescription'></div>
                    <div class='modal-price-section'>
                        <h3>Precios en Tiendas:</h3>
                        <div id='modalPrices'></div>
                    </div>
                    <div id='modalDetails'></div>
                </div>
            </div>
        </div>";
    }

    private static string GenerarJavaScript()
    {
        return @"
let todosLosJuegos = [];

window.addEventListener('DOMContentLoaded', function() {
    todosLosJuegos = Array.from(document.querySelectorAll('.game-card'));
    poblarFiltros();
});

function poblarFiltros() {
    const plataformas = new Set();
    const generos = new Set();
    const tiendas = new Set();
    
    todosLosJuegos.forEach(card => {
        const plats = card.dataset.plataformas.split(',');
        plats.forEach(p => p && plataformas.add(p.trim()));
        
        // Cambiar 'formato' por 'genero'
        if (card.dataset.genero) {
            const gens = card.dataset.genero.split(',');
            gens.forEach(g => g && generos.add(g.trim()));
        }
        
        const stores = card.dataset.tiendas.split(',');
        stores.forEach(s => s && tiendas.add(s.trim()));
    });
    
    const selectPlataforma = document.getElementById('filterPlataforma');
    plataformas.forEach(p => {
        const option = document.createElement('option');
        option.value = p;
        option.textContent = p;
        selectPlataforma.appendChild(option);
    });
    
    // Cambiar 'filterFormato' por 'filterGenero'
    const selectGenero = document.getElementById('filterGenero');
    generos.forEach(g => {
        const option = document.createElement('option');
        option.value = g;
        option.textContent = g;
        selectGenero.appendChild(option);
    });
    
    const selectTienda = document.getElementById('filterTienda');
    tiendas.forEach(t => {
        const option = document.createElement('option');
        option.value = t;
        option.textContent = t;
        selectTienda.appendChild(option);
    });
}

function aplicarFiltros() {
    const tipo = document.getElementById('filterTipo').value;
    const plataforma = document.getElementById('filterPlataforma').value;
    const genero = document.getElementById('filterGenero').value;  // Cambiar de 'formato' a 'genero'
    const tienda = document.getElementById('filterTienda').value;
    const sortBy = document.getElementById('sortBy').value;
    
    let juegosFiltrados = todosLosJuegos.filter(card => {
        const cumpleTipo = !tipo || card.dataset.tipo === tipo;
        const cumplePlataforma = !plataforma || card.dataset.plataformas.includes(plataforma);
        const cumpleGenero = !genero || card.dataset.genero.includes(genero);  // Cambiar
        const cumpleTienda = !tienda || card.dataset.tiendas.includes(tienda);
        
        return cumpleTipo && cumplePlataforma && cumpleGenero && cumpleTienda;
    });
    
    juegosFiltrados.sort((a, b) => {
        switch(sortBy) {
            case 'nombre':
                return a.dataset.nombre.localeCompare(b.dataset.nombre);
            case 'precio-asc':
                return parseFloat(a.dataset.precio) - parseFloat(b.dataset.precio);
            case 'precio-desc':
                return parseFloat(b.dataset.precio) - parseFloat(a.dataset.precio);
            case 'metacritic':
                return parseFloat(b.dataset.metacritic || 0) - parseFloat(a.dataset.metacritic || 0);
            case 'descuento':
            default:
                return parseFloat(b.dataset.descuento) - parseFloat(a.dataset.descuento);
        }
    });
    
    const grid = document.querySelector('.games-grid');
    grid.innerHTML = '';
    juegosFiltrados.forEach(card => grid.appendChild(card));
}

function limpiarFiltros() {
    document.getElementById('filterTipo').value = '';
    document.getElementById('filterPlataforma').value = '';
    document.getElementById('filterGenero').value = '';  // Cambiar
    document.getElementById('filterTienda').value = '';
    document.getElementById('sortBy').value = 'descuento';
    aplicarFiltros();
}

function cerrarModal() {
    document.getElementById('gameModal').style.display = 'none';
}

document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        cerrarModal();
    }
});
";
    }
}