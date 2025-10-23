using ProgressCalculation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Models.AI
{
    /// <summary>
    /// Utilities for discovering/downloading models from HuggingFace
    /// </summary>
    public class HuggingFaceUtils
    {

        public async Task<List<(string Filename, long FileSize)>> GetPossibleModelsAsync(string modelId, string? token = null)
        {
            var url = $"https://huggingface.co/api/models/{modelId}/revision/main?full=true";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Add("Authorization", $"Bearer {token}");

            using var client = new HttpClient();
            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new UnauthorizedAccessException("Model is gated; provide a valid token.");
                response.EnsureSuccessStatusCode(); // Will throw with details
            }

            var json = await response.Content.ReadAsStringAsync();
            var modelInfo = JsonSerializer.Deserialize<ModelInfo>(json);

            var possibleModels = new List<(string Filename, long FileSize)>();
            foreach (var file in modelInfo.Siblings)
            {
                if (file.Rfilename.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                {
                    // File size from LFS metadata, or 0 if not available
                    long fileSize = file.Lfs?.Size ?? 0;
                    possibleModels.Add((file.Rfilename, fileSize));
                }
            }

            return possibleModels;
        }

        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromHours(6) }; // Longer timeout for large files

        private string ReplaceInvalidFilenameChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public async Task DownloadModelFileAsync(string modelId, string filename, string localDir, IProgress<DownloadProgress>? progressReporter = null, string? token = null, CancellationToken cancellationToken = default)
        {
            // Construct local path, handling subdirs in filename
            var localPath = Path.Combine(
                localDir, 
                modelId.Replace('/', Path.DirectorySeparatorChar), 
                filename.Replace('/', Path.DirectorySeparatorChar)
            );
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)); // Ensure dirs exist

            // Resumable: Get existing size
            long existingSize = File.Exists(localPath) ? new FileInfo(localPath).Length : 0;

            // Build download URL
            var url = $"https://huggingface.co/{modelId}/resolve/main/{filename}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Add("Authorization", $"Bearer {token}");
            if (existingSize > 0)
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingSize, null);

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long totalSize = response.Content.Headers.ContentLength ?? -1;
            if (totalSize != -1 && existingSize > 0)
                totalSize += existingSize; // Adjust for resumable

            var startTime = DateTime.Now; // For ETA
            var fileMode = existingSize > 0 ? FileMode.Append : FileMode.Create;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localPath, fileMode, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long downloaded = existingSize;
            int bytesRead;

            try
            {

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    downloaded += bytesRead;

                    // Report progress if reporter provided and totalSize known
                    if (progressReporter != null && totalSize > 0)
                    {
                        double percent = ProgressCalculator.GetXAsPercentOfYPrecise(downloaded, totalSize);
                        long bytesRemaining = totalSize - downloaded;
                        string eta = ProgressCalculator.GetHumanFriendlyTimeRemaining(startTime, (int)percent); // Use integer percent for ETA

                        progressReporter.Report(new DownloadProgress
                        {
                            Percent = percent,
                            BytesRemaining = bytesRemaining,
                            EstimatedTimeRemaining = eta
                        });
                    }
                }

            } catch (OperationCanceledException cancelException)
            {
                // Download cancelled. Attempt to clean up the file.
                try
                {
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                    }
                } catch (Exception ex)
                {
                    // Ignored.
                }
                throw;
            }
        }

        public class DownloadProgress
        {
            public double Percent { get; set; }
            public long BytesRemaining { get; set; }
            public string EstimatedTimeRemaining { get; set; } = string.Empty;
        }

        #region DTO's (Simplified)
        public class ModelInfo
        {
            [JsonPropertyName("siblings")]
            public List<Sibling> Siblings { get; set; } = new();
        }

        public class Sibling
        {
            [JsonPropertyName("rfilename")]
            public string Rfilename { get; set; } = string.Empty;

            [JsonPropertyName("lfs")]
            public LfsInfo? Lfs { get; set; } // Optional, only for LFS-tracked files
        }

        public class LfsInfo
        {
            [JsonPropertyName("size")]
            public long Size { get; set; } // Size in bytes
        }
        #endregion
    }
}
