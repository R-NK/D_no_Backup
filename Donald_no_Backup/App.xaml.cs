using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Donald_no_Backup
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public async void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length != 0)
            {
                foreach (var str in e.Args)
                {
                    if (str == "start")
                    {
                        MainWindow window = new MainWindow();
                        window.TaskIcon.Visibility = Visibility.Visible;
                        window.CreateMainWindow(window);
                        await window.StartBackupAsync();
                        await Task.Delay(10000);
                        Current.Shutdown();
                    }
                }
            }
            else
            {
                MainWindow window = new MainWindow();
                window.Show();
            }
        }
    }
}
