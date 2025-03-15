using System.Collections.Generic;
using System.Windows;

namespace SiemensTrend.Views.Dialogs
{
    public partial class ProjectSelectionDialog : Window
    {
        public string SelectedProject { get; private set; }

        public ProjectSelectionDialog(List<string> projects)
        {
            InitializeComponent();

            // Заполняем список проектов
            lstProjects.ItemsSource = projects;

            if (projects.Count > 0)
            {
                lstProjects.SelectedIndex = 0;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (lstProjects.SelectedItem != null)
            {
                SelectedProject = lstProjects.SelectedItem.ToString();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Выберите проект из списка.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}