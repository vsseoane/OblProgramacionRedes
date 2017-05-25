using Entity;
using Protocol;
using System;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class ClientRequest
    {

        public static void ListFiles(Socket socket)
        {
            Frame frameRequest = new Frame(CMD.ListFiles, Encoding.ASCII.GetBytes(""));
            FrameUtil.Send(socket, frameRequest);
            Frame frameResponse = FrameUtil.Receive(socket);
            string[] listFiles = GetListFormatted(frameResponse.Data);
            if (!IsEmpty(listFiles))
            {
                System.Console.WriteLine("Archivos:");
                foreach (string file in listFiles)
                {
                    System.Console.WriteLine(file);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("No hay archivos.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void ListUsers(Socket socket)
        {
            Frame frameRequest = new Frame(CMD.ListUsers, Encoding.ASCII.GetBytes(""));
            FrameUtil.Send(socket, frameRequest);
            Frame frameResponse = FrameUtil.Receive(socket);
            string[] listUsers = GetListFormatted(frameResponse.Data);
            if (!IsEmpty(listUsers))
            {
                System.Console.WriteLine("Usuarios: ");
                foreach (string user in listUsers)
                {
                    System.Console.WriteLine(user);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("No hay usuarios.");
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        public static void UploadFile(Socket socket, string fileName, string route)
        {
            string routeFull = route + fileName;
            Frame frameRequest = new Frame(CMD.UploadFile, Encoding.ASCII.GetBytes(""));
            FrameUtil.Send(socket, frameRequest);
            FrameUtil.SendFile(socket, routeFull);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Se ha subido el archivo " + fileName);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void DownloadFile(Socket socket, string route, string fileChosen)
        {
            string routeFull = route + fileChosen;
            Frame frameRequest = new Frame(CMD.DownloadFile, Encoding.ASCII.GetBytes(fileChosen));
            FrameUtil.Send(socket, frameRequest);
            Frame frameResponse = FrameUtil.Receive(socket);
            string response = Encoding.ASCII.GetString(frameResponse.Data, 0, frameResponse.DataLength);

            if (response.Equals("OK"))
            {
                FileTransfer file = FrameUtil.ReceiveFile(socket, route);
            }
            else if (response.Equals("ERROR"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("No existe el archivo.");
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        public static void Notify(Socket socket, string userNotify, string userOrigin, string fileName)
        {
            string request = fileName + "@" + userNotify + "@" + userOrigin;
            Frame frameRequest = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(request));
            FrameUtil.Send(socket, frameRequest);
            Frame frameResponse = FrameUtil.Receive(socket);
            string response = Encoding.ASCII.GetString(frameResponse.Data, 0, frameResponse.DataLength);
            if (response.Equals("OKALL"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("Se le ha notificado a todos los usuarios.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (response.Equals("USERLISTEMPTY"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("No hay otros usuarios activos.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (response.Equals("OKUSER"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("Se le ha notificado al usuario.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (response.Equals("USERNOTEXIST"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("El usuario al que quiere notificar no existe o se desconectó");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (response.Equals("FILENOTEXIST"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("El archivo no existe.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Hubo un error, no se pudo notificar");
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        public static void Exit(Socket socket, string user)
        {
            Frame frameRequest = new Frame(CMD.Exit, Encoding.ASCII.GetBytes(user));
            FrameUtil.Send(socket, frameRequest);
        }

        private static bool IsEmpty(string[] listFiles)
        {
            return listFiles == null || listFiles.Length == 0;
        }

        private static string[] GetListFormatted(byte[] data)
        {
            string[] list = null;
            if (data != null)
            {
                string items = System.Text.Encoding.Default.GetString(data);
                if (!items.Equals(""))
                {
                    list = items.Split(';');
                }
            }
            return list;
        }

    }
}
