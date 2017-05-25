using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerRemotingServices
{
    public class FileServiceRemoting : MarshalByRefObject
    {
        public string uri { get; set; }
        public int port { get; set; }

        public FileServiceRemoting()
        {
            uri = "fileService";
            port = 2224;
        }

        public void AddFile(string user)
        {
            Console.WriteLine(user);
        }
    }
}
