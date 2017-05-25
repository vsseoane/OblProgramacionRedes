using Protocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;
using System.IO;
using System.Threading;
using System.IO.Ports;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {

            int portServer = Int32.Parse(ConfigurationManager.AppSettings["PortServer"].ToString());
            string ipServer = ConfigurationManager.AppSettings["IpServer"].ToString();
            string ipClient = ConfigurationManager.AppSettings["IpClient"].ToString();
            string route = "";
            int portOpen = 0;
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(ipServer), portServer);
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), portOpen);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint localEpNotify = new IPEndPoint(IPAddress.Parse("127.0.0.1"), portOpen);
            Socket socketNotify = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Bind(localEndpoint);
                socket.Connect(serverEndpoint);

                socketNotify.Bind(localEpNotify);
                socketNotify.Connect(serverEndpoint);

                Thread notifies = new Thread(() => HandleNotify(socketNotify));

                notifies.Start();

                Frame frameRequest = null;
                string user = "";
                bool isLogged = false;
                bool isFinished = false;

                while (!isLogged && !isFinished)
                {
                    while (user.Equals(""))
                    {
                        Console.WriteLine("Ingrese su usuario:");
                        user = Console.ReadLine().Trim();
                        Console.Title = user;
                    }

                    string messageToSent = user + "@" + localEpNotify.Port + "@" + ipClient;
                    byte[] userData = Encoding.ASCII.GetBytes(messageToSent);
                    frameRequest = new Frame(CMD.Connect, userData);
                    FrameUtil.Send(socket, frameRequest);
                    Frame frameResponse = FrameUtil.Receive(socket);

                    string isConnected = Encoding.ASCII.GetString(frameResponse.Data, 0, frameResponse.Data.Length);
                    if (isConnected.Equals("OK"))
                    {
                        isLogged = true;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Se ha conectado al servidor.");
                        Console.ForegroundColor = ConsoleColor.White;
                        route = CreateFolderClient(user);
                        Console.WriteLine("Presione una tecla para continuar.");
                        Console.ReadLine();
                        Console.Clear();


                    }
                    else if (isConnected.Equals("EXCEEDED"))
                    {
                        Console.WriteLine("Suficientes usuarios conectados, intente más tarde.");
                        Console.ReadLine();
                        isFinished = true;
                    }
                    else if (isConnected.Equals("REPEAT"))
                    {
                        Console.WriteLine("Ya existe un usuario conectado con ese nombre.");
                        user = "";
                    }
                    else
                    {
                        Console.WriteLine("No se ha podido establecer conexión");
                        isFinished = true;
                    }

                }
                if (isLogged)
                {
                    Menu(socket, user, route);
                }

                DeleteFile(route);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Console.WriteLine("Gracias por usar TIRACAJA, presione una tecla para cerrar.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                if (!route.Equals(""))
                {
                    DeleteFile(route);
                }
                Console.WriteLine("No se ha podido establecer la conexión con el Servidor");
                Console.WriteLine("Gracias por usar TIRACAJA, presione una tecla para cerrar.");
                Console.ReadLine();
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception ex)
                {
                    // esto sucede en caso de que no se haya comenzado la ejecución.
                }
            }
        }

        private static void HandleNotify(Socket socket)
        {
            while (socket.Connected)
            {
                try
                {
                    Frame frameResponse = FrameUtil.Receive(socket);
                    if (frameResponse.CMD == CMD.Exit)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();

                    }
                    else
                    {
                        string messageNotify = Encoding.ASCII.GetString(frameResponse.Data, 0, frameResponse.Data.Length);
                        Console.WriteLine("NOTIFY: " + messageNotify);
                        Console.ReadLine();
                    }
                }
                catch (Exception e)
                {
                    Environment.Exit(0);
                }
            }
        }

        private static void Menu(Socket socket, string user, string route)
        {
            bool exit = false;
            while (!exit)
            {

                Console.WriteLine("*-*-*-*-*-*-*-*-*" + user + " Menu:*-*-*-*-*-*-*-*-*");
                Console.WriteLine("1- Listar Archivos");
                Console.WriteLine("2- Listar Usuarios Activos");
                Console.WriteLine("3- Solicitar Carga de Archivo");
                Console.WriteLine("4- Solicitar Descarga de Archivo");
                Console.WriteLine("5- Notificar a un User");
                Console.WriteLine("6- Salir");
                Console.WriteLine("Ingrese una opcion: ");
                int option = GetOptionCorrect(1, 6);
                CMD command = (CMD)option;
                switch (command)
                {
                    case CMD.ListFiles:
                        ClientRequest.ListFiles(socket);
                        Console.WriteLine("Presione una tecla para continuar.");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    case CMD.ListUsers:
                        ClientRequest.ListUsers(socket);
                        Console.WriteLine("Presione una tecla para continuar.");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    case CMD.UploadFile:
                        string fileName = LookUpFile(route);
                        route = CreateFolderClient(user);
                        if (!fileName.Equals(""))
                        {
                            ClientRequest.UploadFile(socket, fileName, route);
                        }
                        Console.WriteLine("Presione una tecla para continuar.");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    case CMD.DownloadFile:
                        Console.WriteLine("Ingrese el nombre del archivo: ");
                        string selectedFile = Console.ReadLine();
                        route = CreateFolderClient(user);
                        ClientRequest.DownloadFile(socket, route, selectedFile);
                        Console.WriteLine("Presione una tecla para continuar.");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    case CMD.Notify:
                        Notify(socket, user);
                        Console.WriteLine("Presione una tecla para continuar.");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    case CMD.Exit:
                        ClientRequest.Exit(socket, user);
                        exit = true;
                        break;
                }
            }
        }

        private static void Notify(Socket socket, string user)
        {
            Console.WriteLine("1- Notifcar a todos ");
            Console.WriteLine("2- Notificar a usuario ");
            Console.WriteLine("Ingrese una opcion ");
            int optionNotify = GetOptionCorrect(1, 2);
            string userNotify = "";
            int notifyToUser = 2;

            if (optionNotify == notifyToUser)
            {
                Console.WriteLine("Ingrese el nombre del usuario a notificar: ");
                userNotify = Console.ReadLine();
            }

            Console.WriteLine("Ingrese el nombre del archivo: ");
            string fileChosenNotify = Console.ReadLine();

            // Si el userNotify es vacío, debe notificar a todos.
            ClientRequest.Notify(socket, userNotify, user, fileChosenNotify);
        }

        private static string LookUpFile(string route)
        {
            Console.WriteLine("Ingrese el nombre del archivo: ");
            string fileName = Console.ReadLine();
            bool existFile = File.Exists(route + fileName);
            if (existFile)
            {
                return fileName;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No existe el archivo en la ruta: " + route);
                Console.ForegroundColor = ConsoleColor.White;
                return "";
            }
        }

        private static string CreateFolderClient(string nameUser)
        {
            string route = ConfigurationManager.AppSettings["Route"].ToString();
            string routeFull = route + nameUser + "\\";
            bool folderExists = Directory.Exists(routeFull);
            if (!folderExists)
            {
                Directory.CreateDirectory(routeFull);
            }
            return routeFull;
        }

        private static void DeleteFile(string route)
        {
            if (Directory.Exists(route))
            {
                Directory.Delete(route, true);
            }
        }

        private static int GetOptionCorrect(int start, int end)
        {
            int option = -1;
            bool isCorrect = false;
            while (!isCorrect)
            {
                try
                {
                    Console.WriteLine("Ingrese la opcion correcta [" + start + " - " + end + "]: ");
                    option = Int32.Parse(Console.ReadLine());
                    if (option >= start && option <= end)
                    {
                        isCorrect = true;
                    }
                }
                catch (FormatException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Su opcion no es correcta.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            return option;
        }
    }
}
