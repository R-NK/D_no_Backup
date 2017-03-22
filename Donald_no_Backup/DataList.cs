using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Donald_no_Backup
{
    public class DataList : INotifyPropertyChanged
    {
        private string name;
        private string from;
        private string to;

        public string Name
        {
            get { return this.name; }
            set
            {
                if(value == this.name) return;
                this.name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string From
        {
            get { return this.from; }
            set
            {
                if(value == this.from) return;
                this.from = value;
                NotifyPropertyChanged("From");
            }
        }

        public string To
        {
            get { return this.to; }
            set
            {
                if(value == this.to) return;
                this.to = value;
                NotifyPropertyChanged("To");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
