using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyDownloaderManager
{
    public class DownloadStorage
    {
        private const string StoragePath = "downloads.json";

        public List<DownloadItem> Load() =>
            File.Exists(StoragePath)
            ? JsonSerializer.Deserialize<List<DownloadItem>>(File.ReadAllText(StoragePath)) ?? new List<DownloadItem>()
            : new List<DownloadItem>();

        public void Save(List<DownloadItem> items) =>
            File.WriteAllText(StoragePath, JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true}));
    }
}
