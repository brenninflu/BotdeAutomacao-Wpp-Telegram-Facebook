using System.Threading;
using System.Threading.Tasks;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public interface IOfferScrapingService
    {
        OfferPlatform Platform { get; }

        bool CanHandle(string url);

        Task<OfferData?> ProcessAsync(string url, CancellationToken cancellationToken = default);
    }
}
