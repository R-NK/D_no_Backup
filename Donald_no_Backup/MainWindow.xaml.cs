using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Xml.Serialization;

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
                ChangeListSize();
            };
            //ウィンドウのサイズ変更時にカラムの自動調整
            SizeChanged += delegate
            {
                ChangeListSize();
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
            //xml読み込み
            XmlSerializer xml = new XmlSerializer(typeof(ObservableCollection<DataList>));
            using (StreamReader sr = new StreamReader("backup.xml"))
            {
                if (!sr.EndOfStream)
                {
                    DataLists = (ObservableCollection<DataList>)xml.Deserialize(sr);
                }
            }
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
            AddButton.IsEnabled = false;
            foreach (var data in DataLists)
            {
                data.Progress = "待機中…";
            }

            DispatcherTimer dispatcherTimer = new DispatcherTimer();

            foreach (var data in DataLists)
            {
                IProgress<int[]> progress = new Progress<int[]>(count =>
                {
                    data.Progress = count[1] + "/" + count[0];
                    if (TaskIcon.Visibility == Visibility.Visible)
                    {
                        TaskIcon.ToolTipText = count[1] + "/" + count[0];
                    }
                });
                Backup bu = new Backup(data.From, data.To);
                await Task.Run(async () =>
                {
                    await bu.StartAsync(progress, dispatcherTimer);
                });
                data.Progress = "完了";
            }
            TaskIcon.ToolTipText = "バックアップ成功:" + DateTime.Now;
            StartButton.IsEnabled = true;
            AddButton.IsEnabled = true;
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            //xmlに保存
            XmlSerializer xml = new XmlSerializer(typeof(ObservableCollection<DataList>));
            using (StreamWriter sw = new StreamWriter("backup.xml"))
            {
                xml.Serialize(sw, DataLists);
            }
        }

        private void ListDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItem != null)
            {
                DataLists.RemoveAt(listView.SelectedIndex);
            }
        }

        private void ChangeListSize()
        {
            DataGridView.Columns[1].Width = (listView.ActualWidth - DataGridView.Columns[0].Width - DataGridView.Columns[3].Width) / 2;
            DataGridView.Columns[2].Width = listView.ActualWidth - DataGridView.Columns[0].Width - DataGridView.Columns[3].Width - DataGridView.Columns[1].Width - 10;
        }
    }
}
