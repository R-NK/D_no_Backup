using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
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
        private int fileNum;

        public Backup(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
            filesPath = new List<Files>();
            foldersPath = new List<Files>();

        }

        public async Task StartAsync(IProgress<int> progressCount, DispatcherTimer dispatcherTimer)
        {
            //1秒ごとにコピー済みファイル数を報告
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += (s, args) =>
            {
                //progressCount.Report(filesPath.Count);
                progressCount.Report(fileNum);
            };
            dispatcherTimer.Start();
            //await ListFiles(fromPath, toPath);
            await ListFilesAsync(fromPath, toPath);

            dispatcherTimer.Stop();
            //using (StreamWriter sw = new StreamWriter("result.txt"))
            //{
            //    sw.WriteLine(filesPath.Count);
            //    foreach (var file in filesPath)
            //    {
            //        sw.WriteLine(file.fromPath + "," + file.toPath);
            //    }
            //}
        }

        private async Task ListFiles(string fromPath, string toPath)
        {

            //対象ディレクトリのファイルを取得
            IEnumerable<string> Files = Directory.EnumerateFiles(fromPath);
            //対象ディレクトリのフォルダを取得
            IEnumerable<string> folders = Directory.EnumerateDirectories(fromPath);

            foreach (string file in Files)
            {
                try
                {
                    CompareFile(file, toPath + @"\" + Path.GetFileName(file));

                }
                catch (UnauthorizedAccessException)
                {

                }
            }
            foreach (string folder in folders)
            {
                //対象ディレクトリにある全てのフォルダに対してこのメソッドを再帰的に実行
                int index = folder.LastIndexOf(@"\", StringComparison.Ordinal);
                await ListFiles(folder, toPath + @"\" + folder.Substring(index + 1, folder.Length - index - 1));
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

        private async Task ListFilesAsync(string fromPath, string toPath)
        {
            //対象ディレクトリのファイルを取得
            IEnumerable<string> files = Directory.EnumerateFiles(fromPath);
            //対象ディレクトリのフォルダを取得
            IEnumerable<string> folders = Directory.EnumerateDirectories(fromPath);

            try
            {
                await Task.WhenAll(files.Select(async file =>
                {
                    string to = toPath + @"\" + Path.GetFileName(file);
                    if (CheckFile(file, to))
                    {
                        if (!Directory.Exists(toPath))
                        {
                            Directory.CreateDirectory(toPath);
                        }
                        using (FileStream fromStream = File.Open(file, FileMode.Open))
                        {
                            using (FileStream toStream = File.Create(to))
                            {
                                await fromStream.CopyToAsync(toStream);
                                fileNum++;
                            }
                        }
                        File.SetCreationTime(to, File.GetCreationTime(file));
                    }
                }));
            }
            catch (UnauthorizedAccessException)
            {

            }

            await Task.WhenAll(folders.Select(async folder =>
            {
                try
                {
                    //ゴミ箱は除外
                    if (!folder.Contains("RECYCLE.BIN"))
                    {
                        //対象ディレクトリにある全てのフォルダに対してこのメソッドを再帰的に実行
                        int index = folder.LastIndexOf(@"\", StringComparison.Ordinal);
                        await ListFilesAsync(folder, toPath + @"\" + folder.Substring(index + 1, folder.Length - index - 1));
                    }
                }
                catch (UnauthorizedAccessException e)
                {

                }
            }));
        }      

        private bool CheckFile(string fromFilePath, string toFilePath)
        {
            if (!File.Exists(toFilePath))
            {
                return true;
            }
            //元ファイルのFileInfo作成
            FileInfo fromFileInfo = new FileInfo(fromFilePath);
            //比較先ファイルのFileInfo作成
            FileInfo toFileInfo = new FileInfo(toFilePath);
            //まずファイルサイズを比べる
            if (fromFileInfo.Length != toFileInfo.Length)
            {
                return true;
            }
            //次は作成日時
            if (fromFileInfo.CreationTime != toFileInfo.CreationTime)
            {
                return true;
            }
            //たぶん同一ファイル
            return false;
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
