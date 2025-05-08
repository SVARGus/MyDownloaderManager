using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyDownloaderManager
{
    public class DownloadManager
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cstDictionary;
        private readonly DownloadStorage _storage;
        private readonly object _sync = new object();

        public List<DownloadItem> Items { get; private set; }

        public DownloadManager(int maxCouncurrentDownloads)
        {
            _semaphore = new SemaphoreSlim(maxCouncurrentDownloads, maxCouncurrentDownloads);
            _httpClient = new HttpClient();
            _cstDictionary = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            _storage = new DownloadStorage();
            Items = _storage.Load();
        }

        public void AddDownload(string url, string filePath, string fileName, IEnumerable<string> tags)
        {
            var item = new DownloadItem
            {
                Id = Guid.NewGuid(),
                Url = url,
                FilePath = filePath,
                NameFile = fileName,
                Tags = tags.ToList(),
                Status = DownloadStatus.Pending
            };
            lock (_sync)
            {
                Items.Add(item);
                SaveStorage();
            }
        }

        public async Task StartDownloadAsync(Guid id)
        {
            var item = Items.FirstOrDefault(x => x.Id == id);
            if (item == null)
            {
                return;
            }

            if (item.Status == DownloadStatus.Completed)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _cstDictionary[id] = cts;

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                item.Status = DownloadStatus.Downloading;
                item.StartLoading = DateTime.Now;
                SaveStorage();
                await DownloadFileAsync(item, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                item.Status = (item.Status == DownloadStatus.Paused) ? DownloadStatus.Paused : DownloadStatus.Cancelled;
            }
            catch
            {
                item.Status = DownloadStatus.Failed;
            }
            finally
            {
                item.EndLoading = DateTime.Now;
                SaveStorage();
                _semaphore.Release();
                _cstDictionary.TryRemove(id, out _);
            }
        }

        private async Task DownloadFileAsync(DownloadItem item, CancellationToken token)
        {
            var finalPath = Path.Combine(item.FilePath, item.NameFile);
            long existingLength = 0;
            if (File.Exists(finalPath))
            {
                existingLength = new FileInfo(finalPath).Length;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);
            if (existingLength > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existingLength, null);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var totalLength = existingLength;
            if (response.Content.Headers.ContentLength.HasValue)
            {
                totalLength += response.Content.Headers.ContentLength.Value;
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var fileStream = new FileStream(finalPath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;
            long totalRead = existingLength;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                totalRead += bytesRead;
                item.Progress = totalLength > 0 ? (totalRead * 100d / totalLength) : 0;
                SaveStorage();
            }

            item.Progress = 100;
            item.Status = DownloadStatus.Completed;
        }

        public void PauseDownload(Guid id)
        {
            if (_cstDictionary.TryGetValue(id, out var cst))
            {
                var item = Items.First(x => x.Id == id);
                item.Status = DownloadStatus.Paused;
                cst.Cancel();
                SaveStorage();
            }
        }

        public void CancelDownload(Guid id)
        {
            if (_cstDictionary.TryGetValue(id,out var cst))
            {
                cst.Cancel();
                var item = Items.First(x => x.Id == id);
                item.Status = DownloadStatus.Cancelled;
                var path = Path.Combine(item.FilePath, item.NameFile);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                SaveStorage();
            }
        }

        public void RemoveDownload(Guid id, bool deleteFile)
        {
            var item = Items.FirstOrDefault(x => x.Id == id);
            if (item == null)
            {
                return;
            }

            if (_cstDictionary.TryGetValue(id, out var cst))
            {
                cst.Cancel();
                _cstDictionary.TryRemove(id, out _);
            }

            if (deleteFile)
            {
                var path = Path.Combine(item.FilePath, item.NameFile);
                if (File.Exists (path))
                {
                    File.Delete(path);
                }
            }

            lock (_sync)
            {
                Items.Remove(item);
                SaveStorage();
            }
        }

        public List<DownloadItem> SearchByTag(string tag)
        {
            return Items.Where(x => x.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public void RenameFile(Guid id, string newName)
        {
            var item = Items.First(x => x.Id == id);
            var oldPath = Path.Combine(item.FilePath, item.NameFile);
            var newPath = Path.Combine(item.FilePath, newName);
            try
            {
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }
                else
                {
                    MessageBox.Show($"Исходный файл не существует: {oldPath}");
                    return;
                }
                item.NameFile = newName;
                SaveStorage();
            }
            catch (IOException ex) when (ex is DirectoryNotFoundException || ex is DriveNotFoundException)
            {
                MessageBox.Show($"Некорректный путь или диск недоступен: {ex.Message}");
            }
            catch (IOException ex) when (File.Exists(newPath))
            {
                MessageBox.Show($"Файл '{newName}' уже существует в целевой директории");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Доступ запрещен: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переименовании файла: {ex.Message}");
            }
        }

        public void MoveFile(Guid id, string newDirectory)
        {
            var item = Items.First(x => x.Id == id);
            var oldPath = Path.Combine(item.FilePath, item.NameFile);
            var newPath = Path.Combine(newDirectory, item.NameFile);
            try
            {
                if (File.Exists(oldPath))
                {
                    Directory.CreateDirectory(newDirectory);
                    File.Move(oldPath, newPath);
                }
                item.FilePath = newDirectory;
                SaveStorage();
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Некорректный путь директории: {newPath.ToString()}. Ошибка: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Доступ запрещен: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении файла: {ex.Message}");
            }
        }

        private void SaveStorage()
        {
            lock (_sync)
            {
                _storage.Save(Items);
            }
        }
    }
}
