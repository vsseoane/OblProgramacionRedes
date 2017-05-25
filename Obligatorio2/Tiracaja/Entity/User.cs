using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Entity
{
    public class User
    {
        public string Name { get; set; }
        public Socket SocketNotify { get; set; }
        public User(string name, Socket socketToNotify)
        {
            this.Name = name;
            this.SocketNotify = socketToNotify;
        }
        public override bool Equals(Object obj)
        {
            return ((User)obj).Name.ToLower().Equals(this.Name.ToLower());
        }
    }
}
