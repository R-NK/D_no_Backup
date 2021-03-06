﻿using System;
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
        private int[] Nums = {0,0};
        private List<string> errorList = new List<string>();

        public Backup(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
        }

        public async Task StartAsync(IProgress<int[]> progressCount)
        {
            await ListFilesAsync(fromPath, toPath, progressCount);
            
            //例外が一つでも投げられたらerror.txtに保存
            if (errorList.Count > 0)
            {
                File.WriteAllLines(@"error.txt", errorList);
            }
        }

        private async Task ListFilesAsync(string fromPath, string toPath, IProgress<int[]> progressCount)
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
                        Interlocked.Increment(ref Nums[0]);
                        progressCount.Report(Nums);
                        if (!Directory.Exists(toPath))
                        {
                            Directory.CreateDirectory(toPath);
                        }
                        //ファイルコピー
                        await CopyWithBufferAllAsync(file, to, 524288);
                        Interlocked.Increment(ref Nums[1]);
                        progressCount.Report(Nums);
                        //作成日時を元ファイルと同じにする
                        File.SetCreationTime(to, File.GetCreationTime(file));
                    }
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
                        await ListFilesAsync(folder, toPath + @"\" + folder.Substring(index + 1, folder.Length - index - 1), progressCount);
                    }
                }
                catch (UnauthorizedAccessException)
                {

                }
            }));
        }

        private async Task CopyWithBufferAllAsync(string file, string to, int bufSize)
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
