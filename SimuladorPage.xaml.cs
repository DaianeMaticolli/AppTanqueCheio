using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TanqueCheio
{
    public partial class SimuladorPage : ContentPage
    {
        Pin? origemPin;
        Pin? destinoPin;
        Polyline? rotaPolyline;

        public SimuladorPage()
        {
            InitializeComponent();
        }

        private async void OnSimularClicked(object sender, EventArgs e)
        {
            string origem = OrigemEntry.Text?.Trim() ?? "";
            string destino = DestinoEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(origem) || string.IsNullOrEmpty(destino))
            {
                await DisplayAlert("Erro", "Por favor, preencha os campos Origem e Destino.", "OK");
                return;
            }

            try
            {
                Location? posOrigem = await GeocodificarEnderecoOSM(origem);
                Location? posDestino = await GeocodificarEnderecoOSM(destino);

                if (posOrigem is null || posDestino is null)
                {
                    await DisplayAlert("Erro", "Não foi possível localizar um dos endereços informados.", "OK");
                    return;
                }

                mapa.Pins.Clear();
                if (rotaPolyline != null)
                    mapa.MapElements.Remove(rotaPolyline);

                origemPin = new Pin { Label = "Origem", Location = posOrigem, Type = PinType.Place };
                destinoPin = new Pin { Label = "Destino", Location = posDestino, Type = PinType.Place };

                mapa.Pins.Add(origemPin);
                mapa.Pins.Add(destinoPin);

                var center = new Location(
                    (posOrigem.Latitude + posDestino.Latitude) / 2,
                    (posOrigem.Longitude + posDestino.Longitude) / 2);

                mapa.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(50)));

                var rota = await BuscarRotaOSRM(posOrigem, posDestino);

                if (rota == null)
                {
                    await DisplayAlert("Erro", "Não foi possível calcular a rota.", "OK");
                    return;
                }

                rotaPolyline = new Polyline
                {
                    StrokeColor = Microsoft.Maui.Graphics.Colors.Blue,
                    StrokeWidth = 5
                };
                foreach (var ponto in rota.Pontos)
                {
                    rotaPolyline.Geopath.Add(ponto);
                }
                mapa.MapElements.Add(rotaPolyline);

                DistanciaEntry.Text = rota.DistanciaKm.ToString("F2");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Falha ao simular viagem: {ex.Message}", "OK");
            }
        }

        private async Task<Location?> GeocodificarEnderecoOSM(string endereco)
        {
            using var http = new HttpClient();
            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(endereco)}&format=json&limit=1";
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TanqueCheioApp/1.0");

            var response = await http.GetStringAsync(url);
            var resultados = JsonDocument.Parse(response).RootElement;

            if (resultados.GetArrayLength() == 0)
                return null;

            var primeiro = resultados[0];
            double lat = double.Parse(primeiro.GetProperty("lat").GetString()!);
            double lon = double.Parse(primeiro.GetProperty("lon").GetString()!);

            return new Location(lat, lon);
        }

        private async Task<RotaInfo?> BuscarRotaOSRM(Location origem, Location destino)
        {
            using var http = new HttpClient();
            string url = $"https://router.project-osrm.org/route/v1/driving/{origem.Longitude},{origem.Latitude};{destino.Longitude},{destino.Latitude}?overview=full&geometries=polyline";

            var response = await http.GetStringAsync(url);
            var doc = JsonDocument.Parse(response);

            if (doc.RootElement.GetProperty("code").GetString() != "Ok")
                return null;

            var rota = doc.RootElement.GetProperty("routes")[0];
            double distanciaMetros = rota.GetProperty("distance").GetDouble();
            string polylineEncoded = rota.GetProperty("geometry").GetString() ?? "";

            var pontos = DecodificarPolyline(polylineEncoded);

            return new RotaInfo
            {
                DistanciaKm = distanciaMetros / 1000.0,
                Pontos = pontos
            };
        }

        private List<Location> DecodificarPolyline(string encoded)
        {
            var poly = new List<Location>();
            int index = 0, len = encoded.Length;
            int lat = 0, lng = 0;

            while (index < len)
            {
                int b, shift = 0, result = 0;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (b & 0x1F) << shift;
                    shift += 5;
                } while (b >= 0x20);
                int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += dlat;

                shift = 0;
                result = 0;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (b & 0x1F) << shift;
                    shift += 5;
                } while (b >= 0x20);
                int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += dlng;

                poly.Add(new Location(lat / 1E5, lng / 1E5));
            }

            return poly;
        }

        private void OnCalcularClicked(object sender, EventArgs e)
        {
            if (!double.TryParse(DistanciaEntry.Text?.Replace(',', '.'), out double distancia) || distancia <= 0)
            {
                DisplayAlert("Erro", "Distância inválida.", "OK");
                return;
            }
            if (!double.TryParse(ConsumoEntry.Text?.Replace(',', '.'), out double consumo) || consumo <= 0)
            {
                DisplayAlert("Erro", "Consumo médio inválido.", "OK");
                return;
            }
            if (!double.TryParse(PrecoCombustivelEntry.Text?.Replace(',', '.'), out double preco) || preco <= 0)
            {
                DisplayAlert("Erro", "Preço do combustível inválido.", "OK");
                return;
            }

            double custo = (distancia / consumo) * preco;
            ResultadoLabel.Text = $"Custo estimado da viagem: R$ {custo:F2}";
        }
    }

    public class RotaInfo
    {
        public double DistanciaKm { get; set; }
        public List<Location> Pontos { get; set; } = new();
    }
}
