using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class SentOffersService
{
    private readonly string _filePath;
    private readonly TimeSpan _expiry = TimeSpan.FromHours(12);
    private List<SentOffer> _sentOffers = new();
    private readonly object _lock = new object();

    public SentOffersService(string filePath = "sent_offers.json")
    {
        _filePath = filePath;
        Load();
    }

    public bool JaFoiEnviado(string link)
    {
        lock (_lock)
        {
            LimparExpirados();
            return _sentOffers.Any(x => x.Link == link);
        }
    }

    public void RegistrarEnvio(string link)
    {
        lock (_lock)
        {
            _sentOffers.Add(new SentOffer { Link = link, DataEnvio = DateTime.UtcNow });
            Save();
        }
    }

    public void LimparExpirados()
    {
        var cutoff = DateTime.UtcNow - _expiry;
        var before = _sentOffers.Count;
        _sentOffers = _sentOffers.Where(x => x.DataEnvio >= cutoff).ToList();
        if (_sentOffers.Count != before) Save();
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _sentOffers = JsonSerializer.Deserialize<List<SentOffer>>(json) ?? new List<SentOffer>();
            }
            catch
            {
                _sentOffers = new List<SentOffer>();
            }
        }
        else
        {
            _sentOffers = new List<SentOffer>();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_sentOffers, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private class SentOffer
    {
        public string Link { get; set; } = string.Empty;
        public DateTime DataEnvio { get; set; }
    }
}
