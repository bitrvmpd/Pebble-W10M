using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Phone.Notification.Management;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace PebbleWuff_10.Models
{
    public interface IApp
    {
        string Name { get; set; }
        string ID { get; set; }
        bool IsRegistered { get; set; }
        ImageSource AppIcon { get; set; }
    }
    public class AppItem : IApp
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public bool IsRegistered { get; set; }
        public ImageSource AppIcon { get; set; }
        public AppItem(string name,string id, WriteableBitmap bmi,bool isRegistered)
        {
            Name = name;
            ID = id;
            IsRegistered = isRegistered;
            AppIcon = bmi;
        }
    }
    public class PebbleAppItem : IApp
    {
        public PebbleAppItem(string name,string id)
        {
            ID = id;
            Name = name;
        }

        public ImageSource AppIcon
        {
            get; set;
        }

        public string ID
        {
            get; set;
        }

        public bool IsRegistered
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }
    }
}
