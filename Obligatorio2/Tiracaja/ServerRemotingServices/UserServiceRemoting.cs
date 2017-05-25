using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerRemotingServices
{
    public class UserServiceRemoting : MarshalByRefObject
    {
        public string uri { get; set; }
        public int port { get; set; }

        public UserServiceRemoting() {
            uri = "userService";
            port = 2223;
        }

        public void AddUser(string user)
        {
            Console.WriteLine(user);
        }
    }
}
