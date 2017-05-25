using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity
{
    public class FileTransfer
    {
        public string Name { get; set; }
        public FileTransfer(string  name)
        {
            this.Name = name;
        }
        public FileTransfer()
        {}
        public override bool Equals(Object obj)
        {
            return ((FileTransfer)obj).Name.ToLower().Equals(this.Name.ToLower());
        }

    }
}
