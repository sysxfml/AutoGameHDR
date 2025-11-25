using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AutoGameHDR
{
    public partial class GlobalListWindow : Window
    {
        public GlobalListWindow(HashSet<string> globalGames)
        {
            InitializeComponent();

            var sortedList = globalGames.OrderBy(x => x).ToList();
            GameList.ItemsSource = sortedList;

            this.Title = $"在线云端白名单 (共 {sortedList.Count} 个)";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}