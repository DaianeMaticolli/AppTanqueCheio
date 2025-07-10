using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using TanqueCheio.Models;
using Microsoft.Maui.Storage;

namespace TanqueCheio.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        const string CHAVE_HISTORICO = "historico_abastecimentos";

        private decimal _precoGasolinaComum;
        public decimal PrecoGasolinaComum
        {
            get => _precoGasolinaComum;
            set { _precoGasolinaComum = value; OnPropertyChanged(); }
        }

        private decimal _precoEtanol;
        public decimal PrecoEtanol
        {
            get => _precoEtanol;
            set { _precoEtanol = value; OnPropertyChanged(); }
        }

        private decimal _valorParaAbastecer;
        public decimal ValorParaAbastecer
        {
            get => _valorParaAbastecer;
            set { _valorParaAbastecer = value; OnPropertyChanged(); }
        }

        private string _sugestao;
        public string Sugestao
        {
            get => _sugestao;
            set { _sugestao = value; OnPropertyChanged(); }
        }

        private string _economia;
        public string Economia
        {
            get => _economia;
            set { _economia = value; OnPropertyChanged(); }
        }

        private string _filtroHistorico;
        public string FiltroHistorico
        {
            get => _filtroHistorico;
            set
            {
                _filtroHistorico = value;
                OnPropertyChanged();
                AplicarFiltroHistorico();
            }
        }

        private ObservableCollection<ComparacaoModel> _todosHistoricos = new();
        private ObservableCollection<ComparacaoModel> _historicoFiltrado = new();
        public ObservableCollection<ComparacaoModel> HistoricoComparacoes
        {
            get => _historicoFiltrado;
            private set { _historicoFiltrado = value; OnPropertyChanged(); }
        }

        private int _itensExibidos = 10;

        public ICommand CompararCommand { get; }
        public ICommand LimparCommand { get; }
        public ICommand RemoverItemCommand { get; }
        public ICommand MostrarMaisCommand { get; }
        public ICommand ExportarPdfCommand { get; }

        public MainViewModel()
        {
            CompararCommand = new Command(Comparar);
            LimparCommand = new Command(Limpar);
            RemoverItemCommand = new Command<ComparacaoModel>(RemoverItem);
            MostrarMaisCommand = new Command(MostrarMais);
            ExportarPdfCommand = new Command(ExportarPdf);

            CarregarHistorico();
        }

        private void Comparar()
        {
            if (PrecoGasolinaComum <= 0 || PrecoEtanol <= 0 || ValorParaAbastecer <= 0)
            {
                Sugestao = "❌ Preencha todos os campos com valores válidos.";
                Economia = "";
                return;
            }

            var comp = new ComparacaoModel
            {
                PrecoGasolinaComum = PrecoGasolinaComum,
                PrecoEtanol = PrecoEtanol,
                ValorParaAbastecer = ValorParaAbastecer,
                DataComparacao = DateTime.Now
            };

            bool etanolCompensa = comp.PrecoEtanol <= comp.PrecoGasolinaComum * 0.7m;

            decimal litrosGasolina = comp.ValorParaAbastecer / comp.PrecoGasolinaComum;
            decimal litrosEtanol = comp.ValorParaAbastecer / comp.PrecoEtanol;

            if (etanolCompensa)
            {
                Sugestao = "💡 O Etanol é mais vantajoso.";
                Economia = $"Você abastece {(litrosEtanol - litrosGasolina):F2} litros a mais com Etanol.";
            }
            else
            {
                Sugestao = "💡 A Gasolina Comum é mais vantajosa.";
                Economia = $"Você abastece {(litrosGasolina - litrosEtanol):F2} litros a mais com Gasolina Comum.";
            }

            _todosHistoricos.Insert(0, comp);

            if (_todosHistoricos.Count > 100) // limite para memória
                _todosHistoricos.RemoveAt(_todosHistoricos.Count - 1);

            AplicarFiltroHistorico();
            SalvarHistorico();
        }

        private void RemoverItem(ComparacaoModel item)
        {
            if (item == null) return;

            _todosHistoricos.Remove(item);
            AplicarFiltroHistorico();
            SalvarHistorico();
        }

        private void MostrarMais()
        {
            _itensExibidos += 10;
            AplicarFiltroHistorico();
        }

        private void AplicarFiltroHistorico()
        {
            if (string.IsNullOrWhiteSpace(FiltroHistorico))
            {
                HistoricoComparacoes = new ObservableCollection<ComparacaoModel>(_todosHistoricos.Take(_itensExibidos));
            }
            else
            {
                var filtroMinusculo = FiltroHistorico.ToLowerInvariant();
                var filtrados = _todosHistoricos.Where(c =>
                    c.Resultado.ToLowerInvariant().Contains(filtroMinusculo) ||
                    c.DataComparacao.ToString("dd/MM/yyyy HH:mm").Contains(filtroMinusculo)
                );

                HistoricoComparacoes = new ObservableCollection<ComparacaoModel>(filtrados.Take(_itensExibidos));
            }
        }

        private void Limpar()
        {
            PrecoGasolinaComum = 0;
            PrecoEtanol = 0;
            ValorParaAbastecer = 0;
            Sugestao = "";
            Economia = "";
        }

        public void LimparHistorico()
        {
            _todosHistoricos.Clear();
            AplicarFiltroHistorico();
            SalvarHistorico();
        }

        private void CarregarHistorico()
        {
            try
            {
                var json = Preferences.Get(CHAVE_HISTORICO, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var lista = JsonSerializer.Deserialize<ObservableCollection<ComparacaoModel>>(json);
                    if (lista != null)
                    {
                        _todosHistoricos = new ObservableCollection<ComparacaoModel>(lista.OrderByDescending(x => x.DataComparacao));
                        _itensExibidos = 10;
                        AplicarFiltroHistorico();
                    }
                }
            }
            catch
            {
                // Ignora erros no carregamento
            }
        }

        private void SalvarHistorico()
        {
            try
            {
                var json = JsonSerializer.Serialize(_todosHistoricos);
                Preferences.Set(CHAVE_HISTORICO, json);
            }
            catch
            {
                // Ignora erros no salvamento
            }
        }

        private async void ExportarPdf()
        {
            try
            {
                var linhas = _todosHistoricos.Select(c =>
                    $"{c.DataComparacao:dd/MM/yyyy HH:mm} - Gasolina: R$ {c.PrecoGasolinaComum:F2} | Etanol: R$ {c.PrecoEtanol:F2} | Valor: R$ {c.ValorParaAbastecer:F2} → {c.Resultado}");

                string textoPdf = string.Join(Environment.NewLine, linhas);

                // TODO: implementar geração real do PDF usando biblioteca compatível
                await App.Current.MainPage.DisplayAlert("Exportar PDF", "Função de exportação para PDF será implementada aqui.", "OK");
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Erro", "Falha ao exportar PDF: " + ex.Message, "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string nome = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
