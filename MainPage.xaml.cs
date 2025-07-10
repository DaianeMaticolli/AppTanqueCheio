using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TanqueCheio
{
    public partial class MainPage : ContentPage
    {
        private const string CHAVE_HISTORICO = "historico_abastecimentos";
        private const decimal LIMITE_ETANOL_GASOLINA = 0.7m;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCompararClicked(object sender, EventArgs e)
        {
            if (!decimal.TryParse(GasolinaEntry.Text?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precoGasolina) || precoGasolina <= 0)
            {
                await DisplayAlert("Erro de Entrada", "Por favor, insira um preço válido para a Gasolina.", "OK");
                GasolinaEntry.Text = string.Empty;
                GasolinaEntry.Focus();
                return;
            }

            if (!decimal.TryParse(EtanolEntry.Text?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precoEtanol) || precoEtanol <= 0)
            {
                await DisplayAlert("Erro de Entrada", "Por favor, insira um preço válido para o Etanol.", "OK");
                EtanolEntry.Text = string.Empty;
                EtanolEntry.Focus();
                return;
            }

            bool usarEtanol = precoEtanol <= precoGasolina * LIMITE_ETANOL_GASOLINA;
            string melhorCombustivel = usarEtanol ? "Etanol" : "Gasolina";
            string mensagemResultado = $"Melhor opção: {melhorCombustivel}";

            ResultadoLabel.Text = mensagemResultado;
            ResultadoLabel.TextColor = usarEtanol ? Color.FromHex("#2E7D32") : Color.FromHex("#FF8C00");
            ResultadoLabel.FontAttributes = FontAttributes.Bold;

            UltimaComparacaoLabel.Text = $"Última comparação: Gasolina R$ {precoGasolina:F2} x Etanol R$ {precoEtanol:F2} -> {melhorCombustivel}";

            var registroHistorico = new ComparacaoHistorico
            {
                DataComparacao = DateTime.Now,
                PrecoGasolinaComum = precoGasolina,
                PrecoEtanol = precoEtanol,
                Resultado = mensagemResultado
            };

            await SalvarNoHistoricoAsync(registroHistorico);
        }

        private void OnLimparClicked(object sender, EventArgs e)
        {
            GasolinaEntry.Text = string.Empty;
            EtanolEntry.Text = string.Empty;
            ResultadoLabel.Text = string.Empty;
            UltimaComparacaoLabel.Text = string.Empty;
        }

        private async Task SalvarNoHistoricoAsync(ComparacaoHistorico novaEntrada)
        {
            string historicoJson = Preferences.Get(CHAVE_HISTORICO, "[]");
            List<ComparacaoHistorico> listaHistorico;

            try
            {
                listaHistorico = System.Text.Json.JsonSerializer.Deserialize<List<ComparacaoHistorico>>(historicoJson)
                                 ?? new List<ComparacaoHistorico>();
            }
            catch
            {
                listaHistorico = new List<ComparacaoHistorico>();
            }

            listaHistorico.Add(novaEntrada);

            if (listaHistorico.Count > 50)
                listaHistorico = listaHistorico.Skip(listaHistorico.Count - 50).ToList();

            string novoJson = System.Text.Json.JsonSerializer.Serialize(listaHistorico);
            Preferences.Set(CHAVE_HISTORICO, novoJson);
        }

        private async void OnVerHistoricoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HistoricoPage());
        }

        private async void OnPostosProximosClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permissão Necessária", "Conceda permissão de localização para ver postos próximos.", "OK");
                        return;
                    }
                }

                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

                if (location == null)
                {
                    await DisplayAlert("Erro", "Não foi possível obter sua localização. Verifique o GPS.", "OK");
                    return;
                }

                string url = $"https://www.google.com/maps/search/?api=1&query=posto+de+gasolina+perto+de+{location.Latitude},{location.Longitude}";

                if (await Launcher.Default.CanOpenAsync(url))
                    await Launcher.Default.OpenAsync(url);
                else
                    await DisplayAlert("Erro", "Não foi possível abrir o navegador para exibir os postos.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro inesperado: {ex.Message}", "OK");
            }
        }

        private async void OnCompartilharClicked(object sender, EventArgs e)
        {
            string textoParaCompartilhar = ResultadoLabel.Text;

            if (string.IsNullOrWhiteSpace(textoParaCompartilhar))
            {
                await DisplayAlert("Atenção", "Não há resultado para compartilhar.", "OK");
                return;
            }

            try
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = textoParaCompartilhar,
                    Title = "Compartilhar Resultado da Comparação"
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Não foi possível compartilhar: {ex.Message}", "OK");
            }
        }
    }

    public class ComparacaoHistorico
    {
        public DateTime DataComparacao { get; set; }
        public decimal PrecoGasolinaComum { get; set; }
        public decimal PrecoEtanol { get; set; }
        public string Resultado { get; set; }
    }
}
