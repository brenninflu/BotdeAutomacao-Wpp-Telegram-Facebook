using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OfertaBot.Services;

namespace OfertaBot.Services
{
    public class VideoGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _tempImagesDir;
        private readonly string _videosDir;

        public VideoGenerationService()
        {
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _tempImagesDir = Path.Combine(Path.GetTempPath(), "OfertaBotImages");
            _videosDir = Config.VideosFolder;
            Directory.CreateDirectory(_tempImagesDir);
            Directory.CreateDirectory(_videosDir);
        }

        public async Task<string?> GenerateVideoAsync(string productName, decimal currentPrice, int discount, List<string> imageUrls, string affiliateLink)
        {
            try
            {
                Console.WriteLine("[VIDEO] Starting video generation for: " + productName);

                // Download images
                var downloadedImages = await DownloadImagesAsync(imageUrls);
                if (!downloadedImages.Any())
                {
                    Console.WriteLine("[VIDEO] Using fallback image");
                    // fallback image
                    var fallbackUrl = "https://picsum.photos/800/600";
                    try
                    {
                        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var response = await _httpClient.GetAsync(fallbackUrl, cts.Token);
                        if (response.IsSuccessStatusCode)
                        {
                            var fallbackPath = Path.Combine(_tempImagesDir, "fallback.jpg");
                            using var fs = new FileStream(fallbackPath, FileMode.Create);
                            await response.Content.CopyToAsync(fs);
                            downloadedImages.Add(fallbackPath);
                        }
                        else
                        {
                            Console.WriteLine($"[ERROR] Fallback image failed: {fallbackUrl} - Status: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Fallback image failed: {fallbackUrl} - {ex.Message}");
                    }
                }
                if (!downloadedImages.Any())
                {
                    Console.WriteLine("[ERROR] No images available, cannot generate video. Skipping video generation.");
                    // Still return null, as no video can be generated
                    return null;
                }

                // Calculate original price
                decimal originalPrice = currentPrice / (1 - (decimal)discount / 100);

                // Generate text overlays
                var textLines = new[]
                {
                    "🔥 PROMOÇÃO IMPERDÍVEL",
                    $"💰 DE R${originalPrice:F2} POR R${currentPrice:F2}",
                    "⚠️ ÚLTIMAS UNIDADES"
                };

                // Create video segments
                var segmentFiles = new List<string>();
                double durationPerImage = 2.5; // seconds
                for (int i = 0; i < downloadedImages.Count; i++)
                {
                    var segmentPath = Path.Combine(_tempImagesDir, $"segment_{i}.mp4");
                    await CreateImageVideoSegmentAsync(downloadedImages[i], segmentPath, durationPerImage, textLines);
                    segmentFiles.Add(segmentPath);
                }

                // Concatenate segments
                var concatVideoPath = Path.Combine(_tempImagesDir, "concat.mp4");
                await ConcatenateVideosAsync(segmentFiles, concatVideoPath);

                // Add background music
                var finalVideoPath = GenerateVideoFileName(productName);
                await AddBackgroundMusicAsync(concatVideoPath, finalVideoPath);

                Console.WriteLine($"[VIDEO] Saved at: {finalVideoPath}");

                // Cleanup temp files
                CleanupTempFiles(downloadedImages.Concat(segmentFiles).Append(concatVideoPath));

                return finalVideoPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VIDEO] Error generating video: {ex.Message}");
                return null;
            }
        }

        private async Task<List<string>> DownloadImagesAsync(List<string> urls)
        {
            Console.WriteLine("[VIDEO] Downloading images");
            var downloaded = new List<string>();
            int count = 0;
            var fallbackImages = new List<string> {
                Path.Combine(AppContext.BaseDirectory, "assets", "fallback1.jpg"),
                Path.Combine(AppContext.BaseDirectory, "assets", "fallback2.jpg")
            };
            foreach (var url in urls.Take(5)) // max 5
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine($"[DOWNLOAD] URL inválida: {url}");
                    continue;
                }
                bool success = false;
                if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        try
                        {
                            Console.WriteLine($"[DOWNLOAD] Baixando imagem: {url} (tentativa {attempt})");
                            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                            var response = await _httpClient.GetAsync(url, cts.Token);
                            if (response.IsSuccessStatusCode)
                            {
                                var imagePath = Path.Combine(_tempImagesDir, $"{count}.jpg");
                                using var fs = new FileStream(imagePath, FileMode.Create);
                                await response.Content.CopyToAsync(fs);
                                downloaded.Add(imagePath);
                                count++;
                                Console.WriteLine($"[IMAGE] Download: {url}");
                                success = true;
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"[DOWNLOAD] Falha: {url} - Status: {response.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[RETRY] Erro ao baixar {url} (tentativa {attempt}): {ex.Message}");
                            if (attempt == 3)
                                Console.WriteLine($"[DOWNLOAD] Falha definitiva para {url}");
                        }
                    }
                }
                else if (File.Exists(url))
                {
                    var destPath = Path.Combine(_tempImagesDir, $"{count}.jpg");
                    File.Copy(url, destPath, true);
                    downloaded.Add(destPath);
                    count++;
                    Console.WriteLine($"[IMAGE] Local: {url}");
                    success = true;
                }
                if (!success)
                {
                    Console.WriteLine($"[FALLBACK] Usando imagem fallback local");
                    if (fallbackImages.Count > 0)
                    {
                        var fallback = fallbackImages[count % fallbackImages.Count];
                        if (File.Exists(fallback))
                        {
                            downloaded.Add(fallback);
                            count++;
                        }
                    }
                }
            }
            if (!downloaded.Any())
            {
                Console.WriteLine("[FALLBACK] Nenhuma imagem baixada, usando todas as imagens fallback");
                foreach (var fallback in fallbackImages)
                {
                    if (File.Exists(fallback))
                        downloaded.Add(fallback);
                }
            }
            Console.WriteLine($"[VIDEO] Downloaded {downloaded.Count} images");
            return downloaded;
        }

        private async Task CreateImageVideoSegmentAsync(string imagePath, string outputPath, double duration, string[] textLines)
        {
            var vf = $"scale=1080:1920";
            for (int i = 0; i < textLines.Length; i++)
            {
                vf += $",drawtext=text='{textLines[i]}':fontcolor=white:fontsize=48:box=1:boxcolor=black@0.5:boxborderw=5:x=(w-text_w)/2:y=100+{i*80}";
            }

            var args = $"-loop 1 -i \"{imagePath}\" -t {duration} -vf \"{vf}\" -c:v libx264 -preset fast -crf 22 -pix_fmt yuv420p \"{outputPath}\"";
            await RunFfmpegAsync(args);
        }

        private async Task ConcatenateVideosAsync(List<string> segmentPaths, string outputPath)
        {
            var listFile = Path.Combine(_tempImagesDir, "list.txt");
            await File.WriteAllLinesAsync(listFile, segmentPaths.Select(p => $"file '{p}'"));
            var args = $"-f concat -safe 0 -i \"{listFile}\" -c copy \"{outputPath}\"";
            await RunFfmpegAsync(args);
            File.Delete(listFile);
        }

        private async Task AddBackgroundMusicAsync(string videoPath, string outputPath)
        {
            var musicPath = Config.BackgroundMusicPath;
            if (!File.Exists(musicPath))
            {
                Console.WriteLine("[VIDEO] Background music not found, copying video without audio");
                File.Copy(videoPath, outputPath);
                return;
            }
            var args = $"-i \"{videoPath}\" -i \"{musicPath}\" -c:v copy -c:a aac -shortest \"{outputPath}\"";
            await RunFfmpegAsync(args);
        }

        private async Task RunFfmpegAsync(string args)
        {
            var ffmpegPath = Config.FfmpegPath;
            Console.WriteLine($"[FFMPEG] Caminho usado: {ffmpegPath}");
            if (!File.Exists(ffmpegPath))
            {
                Console.WriteLine($"[ERROR][FFMPEG] Não encontrado em: {ffmpegPath}");
                throw new FileNotFoundException($"FFmpeg não encontrado em: {ffmpegPath}");
            }
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string stdOut = await process.StandardOutput.ReadToEndAsync();
            string stdErr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Console.WriteLine($"[FFMPEG][STDOUT] {stdOut}");
            Console.WriteLine($"[FFMPEG][STDERR] {stdErr}");
            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg error: {stdErr}");
            }
        }

        private string GenerateVideoFileName(string productName)
        {
            var sanitized = Regex.Replace(productName, @"[^a-zA-Z0-9]", "_");
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(_videosDir, $"{sanitized}_{timestamp}.mp4");
        }

        private void CleanupTempFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                try
                {
                    if (File.Exists(file)) File.Delete(file);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}