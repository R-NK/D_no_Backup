using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Donald_no_Backup
{
    class Backup : MainWindow
    {
        private string fromPath;
        private string toPath;
        private int[] Nums = { 0, 0 };
        private List<string> errorList = new List<string>();

        public Backup(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
        }

        public async Task StartAsync(IProgress<int[]> progressCount, int bufferSize, string startTime)
        {
            await ListFilesAsync(fromPath, toPath, progressCount, bufferSize);

            //例外が一つでも投げられたらerror.txtに保存
            if (errorList.Count > 0)
            {
                File.WriteAllLines($"error_{startTime}.txt", errorList);
            }
        }

        private async Task ListFilesAsync(string fromPath, string toPath, IProgress<int[]> progressCount, int bufferSize)
        {
            //対象ディレクトリのファイルを取得
            IEnumerable<string> files = Directory.EnumerateFiles(fromPath);
            await Task.WhenAll(files.Select(async file =>
            {
                try
                {
                    string to = toPath + @"\" + Path.GetFileName(file);
                    bool isHiden = CheckHidden(file, to);

                    if (CheckFile(file, to))
                    {
                        Interlocked.Increment(ref Nums[0]);
                        progressCount.Report(Nums);
                        if (!Directory.Exists(toPath))
                        {
                            Directory.CreateDirectory(toPath);
                            //作成日時を元フォルダと同じにする
                            Directory.SetCreationTime(toPath, Directory.GetCreationTime(fromPath));
                        }
                        FileInfo fi = new FileInfo(to);
                        //ファイルコピー
                        await CopyWithBufferAllAsync(file, to, bufferSize);
                        if (isHiden)
                        {
                            //隠し属性追加
                            fi.Attributes |= FileAttributes.Hidden;
                        }
                        
                        Interlocked.Increment(ref Nums[1]);
                        progressCount.Report(Nums);

                        //作成日時を元ファイルと同じにする
                        File.SetCreationTime(to, File.GetCreationTime(file));
                        //更新日時を元ファイルと同じにする
                        File.SetLastWriteTime(to, File.GetLastWriteTime(file));
                    }
                }
                catch (Exception e)
                {
                    if (!file.Contains("RECYCLE.BIN"))
                    {
                        errorList.Add(e.ToString());
                    }
                }
            }));
            //更新日時を元フォルダと同じにする
            try
            {
                Directory.SetLastWriteTime(toPath, Directory.GetLastWriteTime(fromPath));
            }
            catch (IOException) { }

            //対象ディレクトリのフォルダを取得
            try
            {
                IEnumerable<string> folders = Directory.EnumerateDirectories(fromPath);
                await Task.WhenAll(folders.Select(async folder =>
                {
                    try
                    {
                        //ゴミ箱は除外
                        if (!folder.Contains("RECYCLE.BIN"))
                        {
                            //対象ディレクトリにある全てのフォルダに対してこのメソッドを再帰的に実行
                            int index = folder.LastIndexOf(@"\", StringComparison.Ordinal);
                            await ListFilesAsync(folder, toPath + @"\" + folder.Substring(index + 1, folder.Length - index - 1), progressCount, bufferSize);
                            //更新日時を元フォルダと同じにする
                            try
                            {
                                Directory.SetLastWriteTime(toPath, Directory.GetLastWriteTime(fromPath));
                            }
                            catch (IOException) { }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }
                }));
            }
            catch (UnauthorizedAccessException e)
            {

            }
        }

        private async Task CopyWithBufferAllAsync(string file, string to, int bufSize)
        {
            try
            {
                using (FileStream inputStream = File.Open(file, FileMode.Open))
                {
                    using (FileStream outputStream = File.Create(to))
                    {
                        byte[] buf;
                        if (inputStream.Length > bufSize)
                        {
                            buf = new byte[bufSize];
                        }
                        else
                        {
                            //入力ファイルが既定バッファより小さい場合そのファイルサイズでバッファを用意
                            buf = new byte[inputStream.Length];
                        }
                        int numBytes;
                        while ((numBytes = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
                        {
                            await outputStream.WriteAsync(buf, 0, numBytes);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            
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
            //比較元がシステムファイルか調べる
            if ((fromFileInfo.Attributes & FileAttributes.System) != 0)
            {
                //飛ばす
                return false;
            }
            //比較元がアクセス可能化調べる
            if ((fromFileInfo.Attributes & FileAttributes.ReadOnly) != 0)
            {
                //読み取り専用属性削除
                fromFileInfo.IsReadOnly = false;
            }
            //比較先がアクセス可能か調べる
            if ((toFileInfo.Attributes & FileAttributes.ReadOnly) != 0)
            {
                //読み取り専用属性削除
                toFileInfo.IsReadOnly = false;
            }
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

        private bool CheckHidden(string fromFilePath, string toFilePath)
        {
            FileInfo fi = new FileInfo(toFilePath);
            if (!fi.Exists)
            {
                return false;
            }
            if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                fi.Attributes &= ~FileAttributes.Hidden;
                return true;
            }
            return false;
        }
    }
}
