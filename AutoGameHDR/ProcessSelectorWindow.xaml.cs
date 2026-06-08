using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
using WinForms = System.Windows.Forms;

namespace AutoGameHDR
{
    public class ProcessItem
    {
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
    }

    public partial class ProcessSelectorWindow : Window
    {
        public string SelectedProcessName { get; private set; }

        public ProcessSelectorWindow()
        {
            InitializeComponent();
            LoadProcesses();
        }

        private void LoadProcesses()
        {
            var list = new List<ProcessItem>();
            var processes = Process.GetProcesses();

            foreach (var p in processes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(p.MainWindowTitle) && !IsIgnored(p.ProcessName))
                    {
                        list.Add(new ProcessItem
                        {
                            ProcessName = p.ProcessName + ".exe",
                            WindowTitle = p.MainWindowTitle
                        });
                    }
                }
                catch { }
            }

            ProcessList.ItemsSource = list.OrderBy(x => x.ProcessName).ToList();
        }

        private bool IsIgnored(string name)
        {
            string lower = name.ToLower();
            return lower == "svchost" || lower == "explorer" || lower == "searchhost" ||
                   lower == "autogamehdr" || lower == "taskmgr" || lower == "applicationframehost";
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var openFileDialog = new WinForms.OpenFileDialog())
                {
                    var app = (App)Application.Current;
                    openFileDialog.Title = app.GetText("Lang_BrowseFileTitle");
                    openFileDialog.Filter = app.GetText("Lang_BrowseFileFilter");
                    openFileDialog.Multiselect = false;
                    openFileDialog.CheckFileExists = true;

                    if (openFileDialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        string fileName = Path.GetFileName(openFileDialog.FileName);
                        SelectedProcessName = fileName;
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                MessageBox.Show(app.GetText("Lang_MsgBrowseBoxFail") + " " + ex.Message, app.GetText("PromptError"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        private void ProcessList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            var item = ProcessList.SelectedItem as ProcessItem;
            if (item != null)
            {
                SelectedProcessName = item.ProcessName;
                DialogResult = true;
                Close();
            }
            else
            {
                var app = (App)Application.Current;
                MessageBox.Show(app.GetText("Lang_MsgNoProcessSelected"), app.GetText("PromptTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}