using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Donald_no_Backup
{
    class Backup
    {
        private string fromPath;
        private string toPath;
        private List<Files> filesPath;

        public Backup(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
            filesPath = new List<Files>();
        }

        public void Start(string fromPath, string toPath)
        {
            

        }

        private void CheckFiles(string frompath, string toPath)
        {
            //対象ディレクトリのファイルを取得
            IEnumerable<string> Files = Directory.EnumerateFiles(fromPath);
            //対象ディレクトリのディレクトリを取得
            IEnumerable<string> folders = Directory.EnumerateDirectories(fromPath);

            foreach (string file in Files)
            {
                filesPath.Add(new Files(file, toPath + Path.GetFileName(file)));
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
