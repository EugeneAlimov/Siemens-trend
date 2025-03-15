using System.Collections.Generic;
using System.Windows;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Views
{
    public partial class ProjectSelectionDialog : Window
    {
        public TiaProjectInfo SelectedProject { get; private set; }

        public ProjectSelectionDialog(List<TiaProjectInfo> projects)
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
                SelectedProject = (TiaProjectInfo)lstProjects.SelectedItem;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Выберите проект из списка.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}