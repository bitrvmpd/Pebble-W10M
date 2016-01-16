/*
    Copyright (C) 2016  Eduardo Elías Noyer Silva

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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
