using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using Hardcodet.Wpf.TaskbarNotification;

namespace Donald_no_Backup
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow ThisWindow;
        public ObservableCollection<DataList> DataLists;
        private SettingData _settingData;
        private int _defaultBuffer = 524288;
        public MainWindow()
        {
            InitializeComponent();

            DataLists = new ObservableCollection<DataList>();
            _settingData = new SettingData();
            //バックアップリストのxml読み込み
            XmlSerializer xml = new XmlSerializer(typeof(ObservableCollection<DataList>));
            try
            {
                using (StreamReader sr = new StreamReader("backup.xml"))
                {
                    if (!sr.EndOfStream)
                    {
                        DataLists = (ObservableCollection<DataList>) xml.Deserialize(sr);
                    }
                }
            }
            catch (IOException)
            {
                StreamWriter sw = new StreamWriter("backup.xml");
                sw.Write("");
            }
            
            //設定のxml読み込み
            XmlSerializer settingxml = new XmlSerializer(typeof(SettingData));
            try
            {
                using (StreamReader sr = new StreamReader("setting.xml"))
                {
                    if (!sr.EndOfStream)
                    {
                        _settingData = (SettingData) settingxml.Deserialize(sr);
                    }
                }
            }
            catch (IOException)
            {
                _settingData.BufferSize = _defaultBuffer;
                _settingData.Schedule = 0;
            }
            

            //タスクトレイから起動されたらトレイアイコンを隠す
            IsVisibleChanged += delegate
            {
                if (TaskIcon.Visibility == Visibility.Visible)
                {
                    TaskIcon.Visibility = Visibility.Hidden;
                }
            };
            
            DataGrid.ItemsSource = DataLists;
        }

        private void AddBackup_Click(object sender, RoutedEventArgs e)
        {
            string[] s = null;
            foreach (var item in AddWindow.ShowCustom(DataLists, s))
            {
                DataLists.Add(item);
            }
        }

        void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var elem = e.MouseDevice.DirectlyOver as FrameworkElement;
            if (elem != null)
            {
                DataGridCell cell = elem.Parent as DataGridCell;
                if (cell == null)
                {
                    // ParentでDataGridCellが拾えなかった時はTemplatedParentを参照
                    // （Borderをダブルクリックした時）
                    cell = elem.TemplatedParent as DataGridCell;
                }
                if (cell != null)
                {
                    var d = (DataList)cell.DataContext;
                    foreach (var list in AddWindow.ShowCustom(DataLists, d.Name, d.From, d.To))
                    {
                        var l = DataLists.FirstOrDefault(i => i.Name == d.Name);
                        l.Name = list.Name;
                        l.From = list.From;
                        l.To = list.To;
                    }
                }
            }
        }

        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            TaskIcon.Visibility = Visibility.Hidden;
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

            await StartBackupAsync();
            
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
            if (DataGrid.SelectedItem != null)
            {
                DataLists.RemoveAt(DataGrid.SelectedIndex);
            }
        }

        public async Task StartBackupAsync()
        {
            TaskIcon.ShowBalloonTip("バックアップ開始", "", BalloonIcon.Info);
            string startTime = "";
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
                    startTime =
                       $"{DateTime.Now.Year}_{DateTime.Now.Month}_{DateTime.Now.Day}_{DateTime.Now.Hour}_{DateTime.Now.Minute}";
                    await bu.StartAsync(progress, _settingData.BufferSize, startTime);
                });
                data.Progress = "完了";
            }
            TaskIcon.ToolTipText = "バックアップ完了:" + DateTime.Now;
            TaskIcon.ShowBalloonTip("バックアップ完了", $"エラー:{ReadError(startTime)}", BalloonIcon.Info);
        }

        private int ReadError(string startTime)
        {
            int errors = 0;
            if (File.Exists($"error_{startTime}.txt"))
            {
                using (StreamReader sr = new StreamReader($"error_{startTime}.txt"))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.Substring(0, 3) != "   " && line.Substring(0, 3) != "---")
                        {
                            errors++;
                        }
                    }
                }
            }
            return errors;
        }

        private void MenuSetting_Click(object sender, RoutedEventArgs e)
        {
            _settingData = Setting.ShowSettingWindow(_settingData);
            //xmlに保存
            XmlSerializer xml = new XmlSerializer(typeof(SettingData));
            using (StreamWriter sw = new StreamWriter("setting.xml"))
            {
                xml.Serialize(sw, _settingData);
            }
        }

        public void CreateMainWindow(MainWindow window)
        {
            ThisWindow = window;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            ThisWindow = this;
            this.Hide();
            TaskIcon.Visibility = Visibility.Visible;
        }
    }
}
