using Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Utils
{
    class FrameUtils
    {
        public static void Send(Socket socket, byte[] data)
        {
            int dataLength = data.Length;
            int sent = 0;
            while (sent < dataLength)
            {
                sent += socket.Send(data, sent, (dataLength - sent), SocketFlags.None);
            }
        }
        public static Frame Received(Socket socket)
        {
            Frame frame = new Frame();
            
               
        }
    }
}
