using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyDownloaderManager;

public partial class MainWindow : Window
{
    private readonly DownloadManager _manager;
    private ICollectionView? _view;

    public MainWindow()
    {
        InitializeComponent();
        _manager = new DownloadManager(maxCouncurrentDownloads: 6);
        _view = CollectionViewSource.GetDefaultView(_manager.Items);
        ListBoxDownloads.ItemsSource = _view;
    }

    private async void ButtonStartDownloading_Click(object sender, RoutedEventArgs e)
    {
        var url = TextBoxUrl.Text.Trim();
        var path = TextBoxFilePath.Text.Trim();
        var name = TextBoxFileName.Text.Trim();
        var tags = TextBoxTags.Text.Split(new[] { ',', ';', ' '}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Заполните URL, путь и имя файла",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (_manager.Items.Any(x => x.FilePath == path && x.NameFile.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Загрузка с таким именем и путём уже есть.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Авто‑добавление расширения, если его нет
        if (!Path.HasExtension(name))
        {
            var ext = Path.GetExtension(new Uri(url).AbsolutePath);
            if (!string.IsNullOrEmpty(ext))
            {
                name += ext;
            }
            else
            {
                MessageBox.Show("Укажите расширение файла.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

            _manager.AddDownload(url, path, name, tags);

        ListBoxDownloads.Items.Refresh();

        var item = _manager.Items.Last();
        await _manager.StartDownloadAsync(item.Id);
        ListBoxDownloads.Items.Refresh();
    }

    private void ButtonMove_Click(object sender, RoutedEventArgs e)
    {
        if (!(AboutDownloaderFile.DataContext is DownloadItem item)) return;

        var dlg = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Выберите новую папку для файла"
        };
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            _manager.MoveFile(item.Id, dlg.FileName);
            ListBoxDownloads.Items.Refresh();
            AboutDownloaderFile.DataContext = item;
        }
    }

    private void ButtonRename_Click(object sender, RoutedEventArgs e)
    {
        if (!(AboutDownloaderFile.DataContext is DownloadItem item))
        {
            return;
        }

        var dlg = new RenameWindow(item.NameFile);
        if (dlg.ShowDialog() == true)
        {
            _manager.RenameFile(item.Id, dlg.NewName);
            ListBoxDownloads.Items.Refresh();
            AboutDownloaderFile.DataContext = item;
        }
    }

    private void ButtonSelectDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Выберите папку"
        };

        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            TextBoxFilePath.Text = dlg.FileName;
        }
    }

    private void ButtonPause_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetItemFromSender(sender, out var item))
        {
            _manager.PauseDownload(item.Id);
            ListBoxDownloads.Items.Refresh();
        }
    }

    private void ButtonResume_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetItemFromSender(sender, out var item))
        {
            Task.Run(async () =>
            {
                await _manager.StartDownloadAsync(item.Id);
                Dispatcher.Invoke(() => ListBoxDownloads.Items.Refresh());
            });
        }
    }

    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetItemFromSender(sender, out var item))
        {
            var result = MessageBox.Show(
                "Удалить информацию о загрузке или вместе с файлом?\n\nДа — только информацию\nНет — вместе с файлом",
                "Подтвердите удаление",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            bool deleteFile = (result == MessageBoxResult.No);
            _manager.RemoveDownload(item.Id, deleteFile);
            ListBoxDownloads.Items.Refresh();
            AboutDownloaderFile.DataContext = null;
            AboutDownloaderFile.IsEnabled = false;
        }
    }

    private async void ButtonRestart_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetItemFromSender(sender, out var item)) return;

        var fullPath = Path.Combine(item.FilePath, item.NameFile);
        if (File.Exists(fullPath))
        {
            var dlg = MessageBox.Show(
                $"Файл уже существует:\n{fullPath}\n\nУдалить и перезапустить?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (dlg == MessageBoxResult.Yes)
                File.Delete(fullPath);
            else
                return;
        }

        item.Progress = 0;
        item.Status = DownloadStatus.Pending;
        item.StartLoading = default;
        item.EndLoading = default;

        try
        {
            await _manager.StartDownloadAsync(item.Id);
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool TryGetItemFromSender(object sender, out DownloadItem? item)
    {
        item = null;
        if (sender is FrameworkElement fe && fe.DataContext is DownloadItem di)
        {
            item = di;
            return true;
        }
        return false;
    }

    private void ListBoxDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ListBoxDownloads.SelectedItem as DownloadItem;
        AboutDownloaderFile.DataContext = selected;
        AboutDownloaderFile.IsEnabled = selected != null;
    }

    private void ButtonSearch_Click(object sender, RoutedEventArgs e)
    {
        var tag = TextBoxSearchTag.Text.Trim();
        if (_view == null) return;
        _view.Filter = o => ((DownloadItem)o).Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }

    private void ButtonClearSearch_Click(object sender, RoutedEventArgs e)
    {
        if (_view == null) return;
        _view.Filter = null;
    }

    private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
    {
        if (!(AboutDownloaderFile.DataContext is DownloadItem item))
        {
            return;
        }
        var input = Microsoft.VisualBasic.Interaction.InputBox(
           "Введите новый тег:", "Добавить тег", "");
        if (!string.IsNullOrWhiteSpace(input))
        {
            item.Tags.Add(input.Trim());
        }
    }
}