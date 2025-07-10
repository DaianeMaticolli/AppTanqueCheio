using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using System.Text.Json;

namespace TanqueCheio
{
    public partial class HistoricoPage : ContentPage
    {
        const string CHAVE_HISTORICO = "historico_abastecimentos";

        private List<ComparacaoHistorico> listaHistorico = new();

        public HistoricoPage()
        {
            InitializeComponent();
            CarregarHistorico();
        }

        private void CarregarHistorico()
        {
            string historicoJson = Preferences.Get(CHAVE_HISTORICO, "[]");

            try
            {
                listaHistorico = JsonSerializer.Deserialize<List<ComparacaoHistorico>>(historicoJson) ?? new List<ComparacaoHistorico>();
            }
            catch
            {
                listaHistorico = new List<ComparacaoHistorico>();
            }

            HistoricoList.ItemsSource = listaHistorico;

            AtualizarResumoAtual();
        }

        private void AtualizarResumoAtual()
        {
            if (listaHistorico.Count == 0)
            {
                ResumoAtualLabel.Text = "Nenhum histórico disponível.";
                return;
            }

            var ultimo = listaHistorico.Last();
            ResumoAtualLabel.Text = $"Última comparação em {ultimo.DataComparacao:dd/MM/yyyy HH:mm}: {ultimo.Resultado}";
        }

        private async void OnVoltarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnLimparHistoricoClicked(object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert("Confirmação", "Deseja limpar todo o histórico?", "Sim", "Não");
            if (confirmar)
            {
                Preferences.Remove(CHAVE_HISTORICO);
                listaHistorico.Clear();
                HistoricoList.ItemsSource = null;
                ResumoAtualLabel.Text = "Nenhum histórico disponível.";
            }
        }

        private async void OnExportarCsvClicked(object sender, EventArgs e)
        {
            if (listaHistorico.Count == 0)
            {
                await DisplayAlert("Aviso", "Nenhum dado no histórico.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Data,Gasolina,Etanol,Melhor");

            foreach (var item in listaHistorico)
            {
                sb.AppendLine($"{item.DataComparacao:yyyy-MM-dd HH:mm},{item.PrecoGasolinaComum:F2},{item.PrecoEtanol:F2},{item.Resultado}");
            }

            string nomeArquivo = $"historico_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            string caminho = Path.Combine(FileSystem.CacheDirectory, nomeArquivo);

            File.WriteAllText(caminho, sb.ToString());

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Exportar CSV",
                File = new ShareFile(caminho)
            });
        }

        private async void OnExportarPdfClicked(object sender, EventArgs e)
        {
            if (listaHistorico.Count == 0)
            {
                await DisplayAlert("Aviso", "Nenhum dado no histórico.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Histórico Tanque Cheio");
            sb.AppendLine(new string('=', 30));
            foreach (var item in listaHistorico)
            {
                sb.AppendLine($"Data: {item.DataComparacao:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"Gasolina: R${item.PrecoGasolinaComum:F2}");
                sb.AppendLine($"Etanol: R${item.PrecoEtanol:F2}");
                sb.AppendLine($"Resultado: {item.Resultado}");
                sb.AppendLine(new string('-', 30));
            }

            string nomeArquivo = $"historico_{DateTime.Now:yyyyMMdd_HHmm}.txt"; // PDF simulado como txt
            string caminho = Path.Combine(FileSystem.CacheDirectory, nomeArquivo);

            File.WriteAllText(caminho, sb.ToString());

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Exportar PDF (simulado como .txt)",
                File = new ShareFile(caminho)
            });
        }
    }
}
