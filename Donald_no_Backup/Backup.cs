using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int fileNum;
        private List<string> result = new List<string>();
        private List<string> errorList = new List<string>();

        public Backup(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
        }

        public async Task StartAsync(IProgress<int> progressCount, DispatcherTimer dispatcherTimer)
        {
            //1秒ごとにコピー済みファイル数を報告
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += (s, args) =>
            {
                progressCount.Report(fileNum);
            };
            dispatcherTimer.Start();
            Stopwatch sw = Stopwatch.StartNew();

            await ListFilesAsync(fromPath, toPath);

            sw.Stop();
            File.WriteAllText(@"time.txt", sw.ElapsedMilliseconds + "ms");
            //File.WriteAllLines(@"result.txt", result);       
            //例外が一つでも投げられたらerror.txtに保存
            if (errorList.Count > 0)
            {
                File.WriteAllLines(@"error.txt", errorList);
            }
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

        private async Task ListFilesAsync(string fromPath, string toPath)
        {
            //対象ディレクトリのファイルを取得
            IEnumerable<string> files = Directory.EnumerateFiles(fromPath);
            //対象ディレクトリのフォルダを取得
            IEnumerable<string> folders = Directory.EnumerateDirectories(fromPath);


            await Task.WhenAll(files.Select(async file =>
            {
                try
                {
                    string to = toPath + @"\" + Path.GetFileName(file);
                    if (CheckFile(file, to))
                    {
                        if (!Directory.Exists(toPath))
                        {
                            Directory.CreateDirectory(toPath);
                        }
                        //ファイルコピー
                        //await CopyWithBuffer(file, to);
                        await CopyAsync(file, to);
                        fileNum++;
                        //作成日時を元ファイルと同じにする
                        File.SetCreationTime(to, File.GetCreationTime(file));
                    }
                    //result.Add(file);
                }
                catch (Exception e)
                {
                    if(!file.Contains("RECYCLE.BIN"))
                    {
                        errorList.Add(e.ToString());
                    }
                }
            }));

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

        private async Task CopyAsync(string file, string to)
        {
            using (FileStream fromStream = File.Open(file, FileMode.Open))
            {
                using (FileStream toStream = File.Create(to))
                {
                    await fromStream.CopyToAsync(toStream);
                }
            }
        }

        private async Task CopyWithBuffer(string file, string to)
        {
            byte[] buf = new byte[10000000];
            using (FileStream inputStream = File.Open(file, FileMode.Open))
            {
                using (FileStream outputStream = File.Create(to))
                {
                    int numBytes;
                    while ((numBytes = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
                    {
                        await outputStream.WriteAsync(buf, 0, numBytes);
                    }
                }
            }
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
}
