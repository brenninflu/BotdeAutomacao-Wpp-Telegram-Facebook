using System.Collections.Generic;

namespace OfertaBot.Models
{
    public class Oferta
    {
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoOriginal { get; set; }
        public decimal PrecoAtual { get; set; }
        public int Desconto { get; set; }

        public string Link { get; set; } = string.Empty;
        public double Avaliacao { get; set; }
        public List<string> Imagens { get; set; } = new List<string>();

        // Cupom (Mercado Livre)
        public bool HasCupom { get; set; }
        public string CupomTipoDesconto { get; set; } = string.Empty; // ex: "50% OFF", "R$20 OFF", "Frete grátis"
        public string CupomCodigo { get; set; } = string.Empty; // ex: "BARAPROMO"
        public decimal CupomValorNumerico { get; set; } // % em inteiro (ex: 50) ou valor em R$
    }
}
