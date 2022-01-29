using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestServerUI
{

    class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    class Users
    {
        public class User : ObservableObject
        {
            public String UserName { get; set; }
            public IPEndPoint UserEndPoint { get; set; }

            public User(String UserName, IPEndPoint UserEndPoint)
            {
                this.UserName = UserName;
                this.UserEndPoint = UserEndPoint;
            }
        }
    }
}
