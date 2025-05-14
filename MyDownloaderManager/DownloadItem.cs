using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class DownloadItem : INotifyPropertyChanged
    {
        public string Url {  get; set; } // Ссылка скачиваемого файла
        public string FilePath { get; set; } // адресс сохранения файла
        public string NameFile { get; set; } // Название сохраненного файла
        public List<string> Tags { get; set; } = new List<string>(); // Указанные теги
        private DownloadStatus _status; // Статус загрузки
        private double _progress; // Процент сохранения 0-100
        public DateTime StartLoading { get; set; } // Дата и время начала загрузки
        public DateTime EndLoading { get; set; } // Дата и время окончания загрузки
        public Guid Id { get; set; } // Уникальный идентификатор
        public event PropertyChangedEventHandler? PropertyChanged;

        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
                }
            }
        }
        public DownloadStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                }
            }
        }
    }
}
