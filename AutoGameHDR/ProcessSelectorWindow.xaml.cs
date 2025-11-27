using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
// 【关键】引入 WinForms 别名
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

        // ===========================
        //  【防崩溃】使用 WinForms 文件浏览框
        // ===========================
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var openFileDialog = new WinForms.OpenFileDialog())
                {
                    openFileDialog.Title = "选择游戏主程序 (.exe)";
                    openFileDialog.Filter = "可执行文件 (*.exe)|*.exe";
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
                MessageBox.Show("打开文件浏览框失败：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("请先选择一个进程！");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}