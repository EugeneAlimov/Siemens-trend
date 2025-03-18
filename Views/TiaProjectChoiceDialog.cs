using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Диалог выбора проекта TIA Portal
    /// </summary>
    public class TiaProjectChoiceDialog : Window
    {
        /// <summary>
        /// Выбранный проект TIA Portal
        /// </summary>
        public TiaProjectInfo SelectedProject { get; private set; }

        /// <summary>
        /// Признак того, что пользователь хочет открыть новый проект
        /// </summary>
        public bool OpenNewProject { get; private set; }

        /// <summary>
        /// Конструктор диалога выбора проекта
        /// </summary>
        /// <param name="projects">Список доступных проектов</param>
        public TiaProjectChoiceDialog(List<TiaProjectInfo> projects)
        {
            // Установка размеров и заголовка
            Title = "Выбор проекта TIA Portal";
            Width = 500;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            // Создание основной сетки
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Создание заголовка
            var headerPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var headerText = new TextBlock
            {
                Text = "Найдены запущенные проекты TIA Portal:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            headerPanel.Children.Add(headerText);

            var subHeaderText = new TextBlock
            {
                Text = "Выберите проект для подключения или откройте новый проект:",
                TextWrapping = TextWrapping.Wrap
            };
            headerPanel.Children.Add(subHeaderText);

            Grid.SetRow(headerPanel, 0);
            grid.Children.Add(headerPanel);

            // Создание списка проектов
            var projectList = new ListBox
            {
                Margin = new Thickness(10),
                DisplayMemberPath = "Name"
            };

            // Добавление проектов в список
            foreach (var project in projects)
            {
                projectList.Items.Add(project);
            }

            // Выбираем первый проект
            if (projectList.Items.Count > 0)
            {
                projectList.SelectedIndex = 0;
            }

            Grid.SetRow(projectList, 1);
            grid.Children.Add(projectList);

            // Создание панели кнопок
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            buttonPanel.Children.Add(cancelButton);

            var openNewButton = new Button
            {
                Content = "Открыть новый",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            openNewButton.Click += (s, e) =>
            {
                OpenNewProject = true;
                DialogResult = true;
                Close();
            };
            buttonPanel.Children.Add(openNewButton);

            var connectButton = new Button
            {
                Content = "Подключиться",
                Width = 100,
                Height = 30,
                IsDefault = true
            };
            connectButton.Click += (s, e) =>
            {
                if (projectList.SelectedItem != null)
                {
                    SelectedProject = projectList.SelectedItem as TiaProjectInfo;
                    OpenNewProject = false;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Выберите проект из списка или нажмите 'Открыть новый'",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            buttonPanel.Children.Add(connectButton);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            // Установка содержимого окна
            Content = grid;
        }
    }
}