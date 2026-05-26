using System;
using System.Collections.Generic;
using System.Linq;

namespace OfertaBot.Services
{
    public class DailyLinkService
    {
        private readonly object _lock = new();
        private readonly LinkedList<string> _recentLinks = new();
        private readonly Dictionary<string, DailyLinkRecord> _records = new(StringComparer.OrdinalIgnoreCase);
        private readonly int _maxRecentLinks;

        private static readonly string[] PopularKeywords = new[]
        {
            "popular",
            "mais vendido",
            "mais vendidos",
            "top",
            "favorito",
            "recomendado",
            "best seller",
            "imperdível",
            "melhor"
        };

        public DailyLinkService()
        {
            _maxRecentLinks = Math.Max(Config.DailyLinkQueueSize, 50);
        }

        public void RecordLink(string url, string messageText)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var normalizedUrl = NormalizeUrl(url);
            if (string.IsNullOrWhiteSpace(normalizedUrl))
                return;

            lock (_lock)
            {
                if (!_records.TryGetValue(normalizedUrl, out var record))
                {
                    record = new DailyLinkRecord(normalizedUrl);
                    _records[normalizedUrl] = record;
                }

                record.Count += 1;
                record.LastSeenUtc = DateTime.UtcNow;
                record.PopularityBoost += ComputeKeywordBoost(messageText);

                if (_recentLinks.Contains(normalizedUrl))
                {
                    _recentLinks.Remove(normalizedUrl);
                }

                _recentLinks.AddFirst(normalizedUrl);

                while (_recentLinks.Count > _maxRecentLinks)
                {
                    var oldest = _recentLinks.Last!.Value;
                    _recentLinks.RemoveLast();
                    CullOldestRecord(oldest);
                }
            }
        }

        public List<string> GetLatestLinks(int count)
        {
            lock (_lock)
            {
                return _recentLinks.Take(count).ToList();
            }
        }

        public List<DailyLinkRecord> GetTopLinks(int count)
        {
            lock (_lock)
            {
                return _records.Values
                    .OrderByDescending(r => r.Score)
                    .ThenByDescending(r => r.LastSeenUtc)
                    .Take(count)
                    .ToList();
            }
        }

        public int GetTrackedCount()
        {
            lock (_lock)
            {
                return _records.Count;
            }
        }

        private void CullOldestRecord(string oldestUrl)
        {
            if (_records.TryGetValue(oldestUrl, out var record) && record.LastSeenUtc < DateTime.UtcNow.AddDays(-7))
            {
                _records.Remove(oldestUrl);
            }
        }

        private static int ComputeKeywordBoost(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return 0;

            var normalized = messageText.Trim().ToLowerInvariant();
            return PopularKeywords.Count(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase)) * 2;
        }

        private static string NormalizeUrl(string url)
        {
            try
            {
                var uri = new UriBuilder(url);
                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in query)
                {
                    if (pair.Key.Equals("utm_source", StringComparison.OrdinalIgnoreCase) ||
                        pair.Key.Equals("utm_medium", StringComparison.OrdinalIgnoreCase) ||
                        pair.Key.Equals("utm_campaign", StringComparison.OrdinalIgnoreCase) ||
                        pair.Key.Equals("affiliate_id", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (pair.Value.Count > 0)
                    {
                        parameters[pair.Key] = pair.Value[0];
                    }
                }

                uri.Query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(string.Empty, parameters).TrimStart('?');
                return uri.ToString();
            }
            catch
            {
                return url.Trim();
            }
        }
    }

    public class DailyLinkRecord
    {
        public DailyLinkRecord(string url)
        {
            Url = url;
            Count = 0;
            LastSeenUtc = DateTime.UtcNow;
            PopularityBoost = 0;
        }

        public string Url { get; }
        public int Count { get; set; }
        public int PopularityBoost { get; set; }
        public DateTime LastSeenUtc { get; set; }

        public double Score => Count * 10 + PopularityBoost + (DateTime.UtcNow - LastSeenUtc).TotalHours < 24 ? 5 : 0;
    }
}
