using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
                    // 过滤逻辑：必须有主窗口标题，且不在黑名单内
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

            // 按首字母排序
            ProcessList.ItemsSource = list.OrderBy(x => x.ProcessName).ToList();
        }

        private bool IsIgnored(string name)
        {
            string lower = name.ToLower();
            return lower == "svchost" || lower == "explorer" || lower == "searchhost" ||
                   lower == "autogamehdr" || lower == "taskmgr" || lower == "applicationframehost";
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