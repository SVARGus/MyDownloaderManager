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
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MyDownloaderManager;

public partial class MainWindow : Window
{
    private readonly DownloadManager _manager;

    public MainWindow()
    {
        InitializeComponent();
        _manager = new DownloadManager(maxCouncurrentDownloads: 6);
        ListBoxDownloads.ItemsSource = _manager.Items;
        AboutDownloaderFile.IsEnabled = false;
    }

    private async Task ButtonStartDownloading_Click(object sender, RoutedEventArgs e)
    {
        var url = TextBoxUrl.Text.Trim();
        var path = TextBoxFilePath.Text.Trim();
        var name = TextBoxFileName.Text.Trim();
        var tags = TextBoxTags.Text.Split(new[] { ',', ';', ' '}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

        _manager.AddDownload(url, path, name, tags);

        ListBoxDownloads.Items.Refresh();

        var item = _manager.Items.Last();
        await _manager.StartDownloadAsync(item.Id);
        ListBoxDownloads.Items.Refresh();
    }

    private void ButtonMove_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ButtonRename_Click(object sender, RoutedEventArgs e)
    {

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

    }

    private void ButtonResume_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ButtonRestart_Click(object sender, RoutedEventArgs e)
    {

    }
}