using System.Collections.Generic;
using System.Linq;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class FiltroService
    {
        public List<Oferta> FiltrarMelhores(List<Oferta> ofertas)
        {
            return ofertas.Where(o => o.Desconto >= 40 && o.Avaliacao >= 4.0 && o.PrecoAtual < o.PrecoOriginal).ToList();
        }
    }
}