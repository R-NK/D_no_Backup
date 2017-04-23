using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Donald_no_Backup
{
    /// <summary>
    /// AddWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AddWindow : Window
    {
        public ObservableCollection<DataList> dataList;
        public ObservableCollection<DataList> oriData;
        public string previewName;

        public AddWindow(ObservableCollection<DataList> recieveData, params string[] ss)
        {
            InitializeComponent();

            if (ss != null)
            {
                NameText.Text = ss[0];
                FromText.Text = ss[1];
                ToText.Text = ss[2];
                AddButton.Content = "保存";
                this.previewName = ss[0];
            }

            this.oriData = recieveData;
            dataList = new ObservableCollection<DataList>();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void From_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog("フォルダ選択");
            // フォルダ選択モード。
            dlg.IsFolderPicker = true;
            var ret = dlg.ShowDialog();
            if (ret == CommonFileDialogResult.Ok)
            {
                FromText.Text = dlg.FileName;
                this.Activate();
            }
        }

        private void To_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog("フォルダ選択");
            // フォルダ選択モード。
            dlg.IsFolderPicker = true;
            var ret = dlg.ShowDialog();
            if (ret == CommonFileDialogResult.Ok)
            {
                ToText.Text = dlg.FileName;
                this.Activate();
            }
        }

        private void Add_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FromText.Text) || string.IsNullOrEmpty(ToText.Text))
            {
                MessageBox.Show("フォルダを選択してください");
                return;
            }
            if (string.IsNullOrEmpty(NameText.Text))
            {
                MessageBox.Show("バックアップ名を入力してください");
                return;
            }
            else if (string.IsNullOrEmpty(previewName))
            {
                foreach (var item in oriData)
                {
                    if (item.Name == NameText.Text)
                    {
                        MessageBox.Show("そのバックアップ名はすでに使用されています");
                        return;
                    }
                }
            }
            else
            {
                foreach (var item in oriData)
                {
                    if (item.Name == NameText.Text && NameText.Text != previewName)
                    {
                        MessageBox.Show("そのバックアップ名はすでに使用されています");
                        return;
                    }
                }
            }
            dataList.Add(new DataList
            {
                Name = NameText.Text,
                From = FromText.Text,
                To = ToText.Text
            });
            this.Close();
        }
        
        static public ObservableCollection<DataList> ShowCustom(ObservableCollection<DataList> recieveData, params string[] recieveString)
        {
            ObservableCollection<DataList> returnList = new ObservableCollection<DataList>();

            AddWindow wnd = new AddWindow(recieveData, recieveString);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.ShowDialog();
            returnList = wnd.dataList;

            return returnList;
        }
    }
}
