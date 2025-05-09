using System.Windows;

namespace MyDownloaderManager
{
    public partial class RenameWindow : Window
    {
        public string NewName { get; private set; }

        public RenameWindow(string currentName)
        {
            InitializeComponent();
            TextBoxNewName.Text = currentName;
            TextBoxNewName.SelectAll();
            TextBoxNewName.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var newName = TextBoxNewName.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Имя не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewName = newName;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
