using System.Windows;

namespace AutoGameHDR
{
    // 用于列表显示的数据模型
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

            // 1. 加载启用列表
            foreach (var game in enabledGames)
            {
                _items.Add(new GameItem { ProcessName = game, IsEnabled = true });
            }

            // 2. 加载禁用列表
            foreach (var game in disabledGames)
            {
                _items.Add(new GameItem { ProcessName = game, IsEnabled = false });
            }

            // 3. 排序并绑定到界面
            _items = _items.OrderBy(x => x.ProcessName).ToList();
            GameGrid.ItemsSource = _items;
        }

        // 删除按钮点击事件
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)sender).DataContext as GameItem;
            if (item != null)
            {
                _items.Remove(item);
                // 刷新 DataGrid 显示
                GameGrid.ItemsSource = null;
                GameGrid.ItemsSource = _items;
            }
        }

        // 保存按钮点击事件
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取主程序实例
            var app = (App)Application.Current;

            // 将修改后的列表传回主程序
            app.UpdateUserList(_items);

            this.Close();
        }
    }
}