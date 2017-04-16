using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Donald_no_Backup
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Window ThisWindow;
        public ObservableCollection<DataList> DataLists;
        public MainWindow()
        {
            InitializeComponent();

            //カラムの自動調整
            Loaded += delegate
            {
                DataGridView.Columns[1].Width = (listView.ActualWidth - DataGridView.Columns[0].Width) / 2;
                DataGridView.Columns[2].Width = listView.ActualWidth - DataGridView.Columns[0].Width - DataGridView.Columns[1].Width -10;
            };
            //ウィンドウのサイズ変更時にカラムの自動調整
            SizeChanged += delegate
            {
                DataGridView.Columns[1].Width = (listView.ActualWidth - DataGridView.Columns[0].Width) / 2;
                DataGridView.Columns[2].Width = listView.ActualWidth - DataGridView.Columns[0].Width - DataGridView.Columns[1].Width -10;
            };
            //タスクトレイから起動されたらトレイアイコンを隠す
            IsVisibleChanged += delegate
            {
                if (TaskIcon.Visibility == Visibility.Visible)
                {
                    TaskIcon.Visibility = Visibility.Hidden;
                }
            };
            DataLists = new ObservableCollection<DataList>();
            listView.ItemsSource = DataLists;
        }



        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ThisWindow = this;
            this.Hide();
            TaskIcon.Visibility = Visibility.Visible;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void AddBackup_Click(object sender, RoutedEventArgs e)
        {
            string[] s = null;
            foreach (var item in AddWindow.ShowCustom(DataLists, s))
            {
                DataLists.Add(item);
            }
        }

        void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listView = (ListView)sender;
            var item = listView.ContainerFromElement((DependencyObject)e.OriginalSource) as ListViewItem;
            if (item != null)
            {
                var d = (DataList)item.DataContext;
                foreach (var list in AddWindow.ShowCustom(DataLists, d.Name, d.From, d.To))
                {
                    var l = DataLists.FirstOrDefault(i => i.Name == d.Name);
                    l.Name = list.Name;
                    l.From = list.From;
                    l.To = list.To;
                }
            }
            else
            {
                MessageBox.Show("Error");
            }
        }

        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            this.Show();

        }

        private void TrayClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void MenuStart_ClickAsync(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            IProgress<int> progressCount = new Progress<int>(count =>
            {
                ProgressLabel.Content = "ファイル列挙中… " + count + "ファイル";
            });
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            Backup bu = new Backup(DataLists[0].From, DataLists[0].To);
            await Task.Run(() => bu.StartAsync(progressCount, dispatcherTimer));
            StartButton.IsEnabled = true;
        }
    }
}
