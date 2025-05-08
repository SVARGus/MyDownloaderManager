using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDownloaderManager
{
    public enum DownloadStatus
    {
        Pending, // Ожидание
        Downloading, // Загрузка
        Completed, // Завершена
        Failed, // Ошибка
        Paused, // Приостоновлена
        Cancelled // Отменена
    }
    public class DownloadItem
    {
        public string Url {  get; set; } // Ссылка скачиваемого файла
        public string FilePath { get; set; } // адресс сохранения файла
        public string NameFile { get; set; } // Название сохраненного файла
        public List<string> Tags { get; set; } = new List<string>(); // Указанные теги
        public DownloadStatus Status { get; set; } = DownloadStatus.Pending; // Статус загрузки
        public double Progress { get; set; } // Процент сохранения 0-100
        public DateTime StartLoading { get; set; } // Дата и время начала загрузки
        public DateTime EndLoading { get; set; } // Дата и время окончания загрузки
        public Guid Id { get; set; } // Уникальный идентификатор
    }
}
