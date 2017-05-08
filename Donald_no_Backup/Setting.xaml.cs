using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32.TaskScheduler;

namespace Donald_no_Backup
{
    /// <summary>
    /// Setting.xaml の相互作用ロジック
    /// </summary>
    public partial class Setting : Window
    {
        private SettingData _settingData;
        public Setting(SettingData oldData)
        {
            InitializeComponent();
            _settingData = oldData;

            BufferSize.Text = _settingData.BufferSize.ToString();
            Interval.Text = _settingData.Interval.ToString();
            TimePicker.Value = _settingData.Time;
            switch (_settingData.Schedule)
            {
                case 0:
                    None.IsChecked = true;
                    break;
                case 1:
                    Hour.IsChecked = true;
                    break;
                case 2:
                    Day.IsChecked = true;
                    break;
                case 3:
                    Week.IsChecked = true;
                    break;
            }
        }

        private void Apply_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _settingData.BufferSize = int.Parse(BufferSize.Text);
                if (Interval.Text != "")
                {
                    _settingData.Interval = int.Parse(Interval.Text);
                }
                if (None.IsChecked == true)
                {
                    _settingData.Schedule = 0;
                }
                if (Hour.IsChecked == true)
                {
                    _settingData.Schedule = 1;
                }
                else if (Day.IsChecked == true)
                {
                    _settingData.Schedule = 2;
                }
                else if (Week.IsChecked == true)
                {
                    _settingData.Schedule = 3;
                }
                _settingData.Time = TimePicker.Value;
                RegisterOrDeleteTask(_settingData);
                this.Close();
            }
            catch (OverflowException)
            {
                MessageBox.Show("値が大きすぎます");
            }
            catch (FormatException)
            {
                MessageBox.Show("無効な値です");
            }
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BufferSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //0~9のみ
            e.Handled = !new Regex("[0-9]").IsMatch(e.Text);
        }

        private void BufferSize_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //貼り付けを許可しない
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void Interval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //0~9のみ
            e.Handled = !new Regex("[0-9]").IsMatch(e.Text);
        }

        private void Interval_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //貼り付けを許可しない
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void taskschedule_Checked(object sender, RoutedEventArgs e)
        {

            var radio = (RadioButton)sender;
            if (radio.Content != null)
            {
                if (radio.Content.ToString() == "時間" | radio.Content.ToString() == "未設定")
                {
                    Interval.IsEnabled = false;
                    IntervalLabel.Content = "";
                }
                else
                {
                    Interval.IsEnabled = true;
                    IntervalLabel.Content = radio.Content.ToString();
                }
            }
        }

        public static SettingData ShowSettingWindow(SettingData oldData)
        {
            Setting wnd = new Setting(oldData);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.ShowDialog();
            return wnd._settingData;
        }

        private void RegisterOrDeleteTask(SettingData settingData)
        {
            const string taskName = "ドナルドのバックアップソフト";
            if (settingData.Schedule != 0)
            {
                TimeTrigger tt = new TimeTrigger();
                //今日の"TimePicker.Value"時に実行
                tt.StartBoundary = DateTime.Today + TimeSpan.FromHours(settingData.Time.Value.Hour);
                if (settingData.Schedule == 1) //時間ごと
                {
                    //24時間ごとに繰り返し 要は毎日
                    tt.Repetition.Interval = TimeSpan.FromHours(24);
                }
                else if (settingData.Schedule == 2) //日ごと
                {
                    tt.Repetition.Interval = TimeSpan.FromDays(int.Parse(Interval.Text));
                }
                else if (settingData.Schedule == 3) //週ごと
                {
                    tt.Repetition.Interval = TimeSpan.FromDays(int.Parse(Interval.Text) * 7);
                }
                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "";
                    td.Principal.LogonType = TaskLogonType.InteractiveToken;
                    td.Triggers.Add(tt);
                    //実行ファイル名取得
                    string name = Assembly.GetExecutingAssembly().Location;
                    //コマンドラインモードで実行 作業ディレクトリは実行ファイルがある場所
                    td.Actions.Add(new ExecAction(name, "start", System.IO.Path.GetDirectoryName(name)));
                    //ルートフォルダにタスクを登録
                    ts.RootFolder.RegisterTaskDefinition(taskName, td);
                }
            }
            else
            {
                using (TaskService ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask(taskName);
                }
            }
        }
    }

    [Serializable]
    public class SettingData
    {
        public int BufferSize { get; set; }
        public int Interval { get; set; }
        public int Schedule { get; set; }
        public DateTime? Time { get; set; }
    }
}
