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

            var app = (App)Application.Current;
            this.Title = $"{app.GetText("Lang_GlobalListTitlePrefix")} ({sortedList.Count})";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}