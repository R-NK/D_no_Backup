using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Donald_no_Backup
{
    class Backup : MainWindow
    {
        private string fromPath;
        private string toPath;
        private List<Files> filesPath;
        private List<Files> foldersPath;

        public Backup(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
            filesPath = new List<Files>();
            foldersPath = new List<Files>();
            
        }

        public async Task StartAsync(IProgress<int> progressCount, DispatcherTimer dispatcherTimer)
        {
            //1秒ごとにコピー予定ファイル数を報告
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += (s, args) =>
            {
                progressCount.Report(filesPath.Count);
            };
            dispatcherTimer.Start();
            await ListFiles(fromPath, toPath);
            dispatcherTimer.Stop();
            using (StreamWriter sw = new StreamWriter("result.txt"))
            {
                sw.WriteLine(filesPath.Count);
                foreach (var file in filesPath)
                {
                    sw.WriteLine(file.fromPath + "," + file.toPath);
                }
            }
        }

        private async Task ListFiles(string fromPath, string toPath)
        {
            try
            {
                //対象ディレクトリのファイルを取得
                IEnumerable<string> Files = Directory.EnumerateFiles(fromPath);
                //対象ディレクトリのフォルダを取得
                IEnumerable<string> folders = Directory.EnumerateDirectories(fromPath);

                foreach (string file in Files)
                {
                    CompareFile(file, toPath + @"\" + Path.GetFileName(file));
                }
                foreach (string folder in folders)
                {
                    //対象ディレクトリにある全てのフォルダに対してこのメソッドを再帰的に実行
                    int index = folder.LastIndexOf(@"\", StringComparison.Ordinal);
                    await ListFiles(folder, toPath + @"\" + folder.Substring(index + 1, folder.Length - index - 1));
                }
            }
            catch (UnauthorizedAccessException)
            {
                
            }
        }

        private void CompareFile(string fromFilePath, string toFilePath)
        {
            //比較先ファイルが無ければリストに追加
            if (!File.Exists(toFilePath))
            {
                filesPath.Add(new Files(fromFilePath, toFilePath));
            }
            else
            {
                //元ファイルのFileInfo作成
                FileInfo fromFileInfo = new FileInfo(fromFilePath);
                //比較先ファイルのFileInfo作成
                FileInfo toFileInfo = new FileInfo(toFilePath);
                //まずファイルサイズを比べる
                if (fromFileInfo.Length != toFileInfo.Length)
                {
                    filesPath.Add(new Files(fromFilePath, toFilePath));
                }
                //次は更新日時
                else if (fromFileInfo.LastWriteTime != toFileInfo.LastWriteTime)
                {
                    filesPath.Add(new Files(fromFilePath, toFilePath));
                }
            }
        }
    }

    class Files
    {
        public string fromPath;
        public string toPath;

        public Files(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
        }
    }
}
