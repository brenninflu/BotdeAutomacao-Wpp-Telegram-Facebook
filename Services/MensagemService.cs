using System.Globalization;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class MensagemService
    {
        public string GerarMensagem(Oferta oferta)
        {
            var preco = oferta.PrecoAtual.ToString("F2", new CultureInfo("pt-BR"));

            if (oferta.HasCupom && !string.IsNullOrWhiteSpace(oferta.CupomTipoDesconto) && !string.IsNullOrWhiteSpace(oferta.CupomCodigo))
            {
                return
                    $"🔥 {oferta.Nome}\n\n" +
                    $"💰 R$ {preco}\n\n" +
                    $"🎟 CUPOM | {oferta.CupomTipoDesconto}\n" +
                    $"👉 CUPOM: {oferta.CupomCodigo}\n\n" +
                    $"{oferta.Link}";
            }

            return
                $"🔥 {oferta.Nome}\n\n" +
                $"💰 R$ {preco}\n\n" +
                $"{oferta.Link}";
        }

        public string GerarMensagem(OfferData oferta)
        {
            var preco = oferta.Price.ToString("F2", new CultureInfo("pt-BR"));

            var baseMsg = $"🔥 {oferta.ProductName}\n\n" +
                          $"💰 R$ {preco}\n\n";

            if (oferta.Coupon != null && (oferta.Coupon.HasCoupon || !string.IsNullOrEmpty(oferta.Coupon.Code)))
            {
                if (!string.IsNullOrEmpty(oferta.Coupon.DiscountType))
                    baseMsg += $"🎟 CUPOM | {oferta.Coupon.DiscountType}\n";
                
                if (!string.IsNullOrEmpty(oferta.Coupon.Code))
                    baseMsg += $"👉 CUPOM: {oferta.Coupon.Code}\n";

                baseMsg += "\n";
            }

            return baseMsg + GetBestLink(oferta);
        }

        public string GerarMensagemTelegramHtml(OfferData oferta)
        {
            return GerarMensagem(oferta);
        }

        private static string GetBestLink(OfferData oferta)
        {
            // Regra absoluta: Priorizar sempre o link original (encurtado/com tracking)
            if (!string.IsNullOrWhiteSpace(oferta.OriginalUrl))
                return oferta.OriginalUrl;

            return oferta.OriginalUrl;
        }
    }
}
