using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    public enum CMD
    {
        Connect, ListFiles, ListUsers, UploadFile, DownloadFile, Notify, Exit
    }
    public class Frame
    {
        public CMD CMD { get; set; }
        public Byte[] Data { get; set; }
        public int DataLength { get; set; }

        public Frame(CMD cmd, Byte[] data)
        {
            CMD = cmd;
            Data = data;
            DataLength = data.Length;
        }

        public Frame()
        {
        }
        
    }
}
