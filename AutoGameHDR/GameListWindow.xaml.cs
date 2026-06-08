using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;

namespace AutoGameHDR
{
    public class GameItem
    {
        public string ProcessName { get; set; }
        public bool IsEnabled { get; set; }
    }

    public partial class GameListWindow : Window
    {
        private List<GameItem> _items = new List<GameItem>();

        public GameListWindow(HashSet<string> enabledGames, HashSet<string> disabledGames)
        {
            InitializeComponent();

            foreach (var game in enabledGames)
            {
                _items.Add(new GameItem { ProcessName = game, IsEnabled = true });
            }

            foreach (var game in disabledGames)
            {
                _items.Add(new GameItem { ProcessName = game, IsEnabled = false });
            }

            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            _items = _items.OrderBy(x => x.ProcessName).ToList();
            GameGrid.ItemsSource = null;
            GameGrid.ItemsSource = _items;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)sender).DataContext as GameItem;
            if (item != null)
            {
                _items.Remove(item);
                RefreshDataGrid();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            app.UpdateUserList(_items);
            this.Close();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var openDialog = new WinForms.OpenFileDialog())
                {
                    var app = (App)Application.Current;
                    openDialog.Title = app.GetText("Lang_ImportTitle");
                    openDialog.Filter = app.GetText("Lang_ImportFilter");

                    if (openDialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        var lines = File.ReadAllLines(openDialog.FileName);
                        int count = 0;

                        foreach (var line in lines)
                        {
                            string cleanName = line.Trim();
                            if (string.IsNullOrWhiteSpace(cleanName))
                            {
                                continue;
                            }

                            if (!_items.Any(x => x.ProcessName.Equals(cleanName, StringComparison.OrdinalIgnoreCase)))
                            {
                                _items.Add(new GameItem { ProcessName = cleanName, IsEnabled = true });
                                count++;
                            }
                        }

                        RefreshDataGrid();
                        MessageBox.Show(string.Format(app.GetText("Lang_MsgImportSuccess"), count), app.GetText("PromptTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                MessageBox.Show(app.GetText("Lang_MsgImportFail") + " " + ex.Message, app.GetText("PromptError"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var saveDialog = new WinForms.SaveFileDialog())
                {
                    var app = (App)Application.Current;
                    saveDialog.Title = app.GetText("Lang_ExportTitle");
                    saveDialog.Filter = app.GetText("Lang_ExportFilter");
                    saveDialog.FileName = "AutoGameHDR_Backup.txt";

                    if (saveDialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        var lines = _items.Select(x => x.ProcessName).ToList();
                        File.WriteAllLines(saveDialog.FileName, lines);

                        MessageBox.Show(app.GetText("Lang_MsgExportSuccess"), app.GetText("PromptTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                MessageBox.Show(app.GetText("Lang_MsgExportFail") + " " + ex.Message, app.GetText("PromptError"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}