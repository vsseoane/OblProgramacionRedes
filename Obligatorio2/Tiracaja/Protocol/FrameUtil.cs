using Entity;
using Protocol;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;


namespace Protocol
{
    public class FrameUtil
    {
        static int BUFFER_LENGTH = 100;

        public static void SendData(Socket socket, byte[] data)
        {
            int dataLength = data.Length;
            int sent = 0;

            while (sent < dataLength)
            {
                sent += socket.Send(data, sent, (dataLength - sent), SocketFlags.None);
            }
        }

        public static void Send(Socket socket, Frame frame)
        {
            int dataLength = frame.DataLength;
            int lengthCMDandLengthFile = 6;
            byte[] informationData = new byte[lengthCMDandLengthFile];
            byte[] cmdBytes = BitConverter.GetBytes((int)frame.CMD);
            byte[] lengthBytes = BitConverter.GetBytes(frame.DataLength);
            int cmdStart = 0;
            int cmdEnd = 2;
            int lengthFileStart = 2;
            int lengthFileEnd = 4;
            Array.Copy(cmdBytes, 0, informationData, cmdStart, cmdEnd);
            Array.Copy(lengthBytes, 0, informationData, lengthFileStart, lengthFileEnd);
            SendData(socket, informationData);
            byte[] data = frame.Data;
            SendSegmented(socket, data);
        }

        public static void SendSegmented(Socket socket, byte[] data)
        {
            byte[] buffer = new Byte[BUFFER_LENGTH];
            int countsFullBuffers = (data.Length) / BUFFER_LENGTH;
            int rest = (data.Length + 1) % BUFFER_LENGTH;

            for (int i = 0; i < countsFullBuffers; i++)
            {
                int start = i * BUFFER_LENGTH;
                Array.ConstrainedCopy(data, start, buffer, 0, buffer.Length - 1);
                SendData(socket, buffer);
            }
            if (rest > 0)
            {
                int start = BUFFER_LENGTH * countsFullBuffers;
                byte[] dataRest = new byte[rest];
                Array.ConstrainedCopy(data, start, dataRest, 0, rest - 1);
                SendData(socket, dataRest);
            }
        }

        public static void SendFile(Socket socket, string routeFull)
        {
            FileInfo fileInfo = new FileInfo(routeFull);
            int lengthFile = (int)fileInfo.Length;
            byte[] lengthFileInByte = BitConverter.GetBytes(lengthFile);
            SendData(socket, lengthFileInByte);

            string fileName = fileInfo.Name;
            int lengthFileName = fileName.Length;
            byte[] lengthFileNameInByte = BitConverter.GetBytes(lengthFileName);
            SendData(socket, lengthFileNameInByte);
            byte[] fileNameInByte = Encoding.ASCII.GetBytes(fileName);
            SendData(socket, fileNameInByte);

            try
            {
                var fileCopy = new byte[BUFFER_LENGTH];
                using (var fileStream = new FileStream(routeFull, FileMode.Open, FileAccess.Read))
                {
                    int read = 0;
                    int maxToCopy = BUFFER_LENGTH;
                    while (read < lengthFile)
                    {
                        maxToCopy = CalculateMaxToCopy(lengthFile, BUFFER_LENGTH, read);
                        if (maxToCopy != BUFFER_LENGTH)
                        {
                            fileCopy = new byte[maxToCopy];
                        }
                        read += fileStream.Read(fileCopy, 0, maxToCopy);
                        SendData(socket, fileCopy);
                    }
                }

            }
            catch (SocketException ex)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Console.WriteLine("El servidor cerró la conexión");
                Console.ReadLine();
            }
        }

        public static void ReceiveData(Socket socket, byte[] data)
        {
            int dataLength = data.Length;
            int received = 0;
            while (received < dataLength)
            {
                received += socket.Receive(data, received, (dataLength - received), SocketFlags.None);
            }
        }

        public static Frame Receive(Socket socket)
        {
            Frame frame = new Frame();
            try
            {
                byte[] informationData = new byte[6];
                ReceiveData(socket, informationData);

                int lengthToCopy = 2;
                int startIndex = 0;
                byte[] commandByte = SubArray(informationData, startIndex, lengthToCopy);
                lengthToCopy = 4;
                startIndex = 2;
                byte[] lengthDataByte = SubArray(informationData, startIndex, lengthToCopy);
                int command = BitConverter.ToInt16(commandByte, 0);
                int lengthData = BitConverter.ToInt32(lengthDataByte, 0);
                frame.CMD = (CMD)command;
                byte[] data = new Byte[lengthData];

                data = ReceivedSegmented(socket, data);
                frame.Data = data;
                frame.DataLength = lengthData;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error en la conexión");
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            return frame;
        }

        public static byte[] ReceivedSegmented(Socket socket, byte[] data)
        {
            byte[] buffer = new Byte[BUFFER_LENGTH];
            int countsFullBuffers = (data.Length) / BUFFER_LENGTH;
            int rest = (data.Length + 1) % BUFFER_LENGTH;

            for (int i = 0; i < countsFullBuffers; i++)
            {
                int start = i * BUFFER_LENGTH;
                ReceiveData(socket, buffer);
                Array.ConstrainedCopy(buffer, 0, data, start, buffer.Length - 1);
            }
            if (rest > 0)
            {
                int start = BUFFER_LENGTH * countsFullBuffers;
                byte[] dataRest = new byte[rest];
                ReceiveData(socket, dataRest);
                Array.ConstrainedCopy(dataRest, 0, data, start, dataRest.Length - 1);
            }


            return data;
        }

        public static FileTransfer ReceiveFile(Socket socket, string route)
        {

            byte[] lengthFileInByte = new byte[4];
            ReceiveData(socket, lengthFileInByte);
            int lengthFile = BitConverter.ToInt32(lengthFileInByte, 0);
            var fileCopy = new byte[BUFFER_LENGTH];

            byte[] lengthFileNameInByte = new byte[4];
            ReceiveData(socket, lengthFileNameInByte);
            int lengthNameFile = BitConverter.ToInt32(lengthFileNameInByte, 0);
            byte[] nameInByte = new byte[lengthNameFile];
            ReceiveData(socket, nameInByte);
            String nameFile = Encoding.UTF8.GetString(nameInByte).TrimEnd();
            nameFile = CheckName(nameFile, route);
            FileTransfer file = new FileTransfer();
            file.Name = nameFile;
            string routeFull = route + nameFile;
            int maxToCopy;
            int write = 0;
            try
            {
                using (var fileStream = new FileStream(routeFull, FileMode.Append, FileAccess.Write))
                {
                    while (write < lengthFile)
                    {
                        maxToCopy = CalculateMaxToCopy(lengthFile, BUFFER_LENGTH, write);
                        if (maxToCopy != BUFFER_LENGTH)
                        {
                            fileCopy = new byte[maxToCopy];
                        }
                        ReceiveData(socket, fileCopy);

                        fileStream.Write(fileCopy, 0, maxToCopy);
                        write += maxToCopy;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("El archivo fue recibido.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("La conexión se cerró.");
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

            }
            return file;
        }

        public static Byte[] SubArray(Byte[] data, int index, int length)
        {
            Byte[] result = new Byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private static int CalculateMaxToCopy(int fileSize, int bufferLength, int read)
        {
            int countNotCopied = fileSize - read;
            if (countNotCopied < bufferLength)
            {
                return countNotCopied;
            }
            return bufferLength;
        }

        private static string CheckName(string nameFile, string route)
        {
            int cont = 0;
            string extension = "";
            bool isWithExt = nameFile.Split('.').Length == 2;
            if (isWithExt)
            {
                extension = nameFile.Split('.')[1];
            }
            while (File.Exists(route + nameFile))
            {
                nameFile = nameFile.Split('.')[0] + cont;
                if (isWithExt)
                {
                    nameFile += "." + extension;
                }
                cont++;
            }
            return nameFile;
        }
   
        
    }
}
