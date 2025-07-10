using System;

namespace TanqueCheio.Models
{
    public class ComparacaoModel
    {
        public decimal PrecoGasolinaComum { get; set; }
        public decimal PrecoEtanol { get; set; }
        public decimal ValorParaAbastecer { get; set; }
        public DateTime DataComparacao { get; set; }

        public string Resultado
        {
            get
            {
                bool etanolCompensa = PrecoEtanol <= PrecoGasolinaComum * 0.7m;
                if (etanolCompensa)
                    return "Etanol mais vantajoso";
                else
                    return "Gasolina Comum mais vantajosa";
            }
        }

        public override string ToString()
        {
            return $"Gasolina: R$ {PrecoGasolinaComum:F2}, Etanol: R$ {PrecoEtanol:F2}, Valor: R$ {ValorParaAbastecer:F2}";
        }
    }
}
