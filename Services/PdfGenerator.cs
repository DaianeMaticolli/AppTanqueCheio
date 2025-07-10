using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TanqueCheio.Services
{
    public static class PdfGenerator
    {
        public static async Task<string> GerarHistoricoPdfAsync(List<string> historico)
        {
            var documento = new PdfDocument();
            var pagina = documento.AddPage();
            var gfx = XGraphics.FromPdfPage(pagina);
            var fonte = new XFont("Verdana", 12);

            int linhaY = 40;

            gfx.DrawString("Histórico de Comparações de Combustíveis",
                new XFont("Verdana", 14, XFontStyle.Bold),
                XBrushes.Black, new XRect(0, 20, pagina.Width, pagina.Height),
                XStringFormats.TopCenter);

            foreach (var item in historico)
            {
                gfx.DrawString(item, fonte, XBrushes.Black,
                    new XRect(40, linhaY, pagina.Width - 80, pagina.Height),
                    XStringFormats.TopLeft);
                linhaY += 20;

                if (linhaY > pagina.Height - 40)
                {
                    pagina = documento.AddPage();
                    gfx = XGraphics.FromPdfPage(pagina);
                    linhaY = 40;
                }
            }

            var nomeArquivo = $"historico_combustivel_{System.DateTime.Now:yyyyMMddHHmmss}.pdf";
            var caminho = Path.Combine(FileSystem.CacheDirectory, nomeArquivo);

            using (var stream = File.Create(caminho))
            {
                documento.Save(stream);
            }

            return caminho;
        }
    }
}
