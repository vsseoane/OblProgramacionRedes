using Protocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Configuration;
using Entity;
using System.IO;
using System.Runtime.Remoting.Channels;
using ServerRemotingServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Http;

namespace Server
{
    class Server
    {

        private static int clientCount = 0;
        private static int COUNTMAX = Int32.Parse(ConfigurationManager.AppSettings["CountMax"].ToString());
        private static Repository lists;

        static void Main(string[] args)
        {
            Console.Title = "Server";
            lists = new Repository();
            int port = Int32.Parse(ConfigurationManager.AppSettings["Port"].ToString());
            string ipServer = ConfigurationManager.AppSettings["Ip"].ToString();
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEp = new IPEndPoint(IPAddress.Parse(ipServer), port);
            serverSocket.Bind(localEp);
            int listenServer = Int32.Parse(ConfigurationManager.AppSettings["ListenServer"].ToString());
            serverSocket.Listen(listenServer);
            bool isFinished = false;
            string route = ConfigurationManager.AppSettings["Route"].ToString();
            DeleteFile(route);
            CreateFolderServer(route);
            ConnectRemotingServices();
            try {
                while (!isFinished)
                {
                    Console.WriteLine("Conectando...");
                    Socket clientSocket = serverSocket.Accept();
                    Socket clientNotify = serverSocket.Accept();
                    Thread clientHandler = new Thread(() => HandleClient(clientSocket, clientNotify));
                    clientHandler.Start();
                }
            }
            catch
            {
                Console.Write("Ha ocurrido algo");
            }
            
        }

        private static void ConnectRemotingServices()
        {
            UserServiceRemoting userService = new UserServiceRemoting();
            ChannelServices.RegisterChannel(GetChannel(userService.port), false);
            RemotingServices.Marshal(userService, userService.uri);
          /*  FileServiceRemoting fileServie = new FileServiceRemoting();
            ChannelServices.RegisterChannel(GetChannel(fileServie.port), false);
            RemotingServices.Marshal(fileServie, fileServie.uri);*/
        }

        private static HttpChannel GetChannel(int httpPort)
        {
            return new HttpChannel(httpPort);
        }
        private static void HandleClient(Socket socket, Socket clientNotify)
        {
            Console.WriteLine("Conectado el cliente " + clientCount);
            Frame frameReceived = null;
            string user = "";
            string route = ConfigurationManager.AppSettings["Route"].ToString();
            bool isSocketClosed = false;
            if (clientCount >= COUNTMAX)
            {

                Console.WriteLine("Error!, Cantidad de usuarios excedidos. ");
                frameReceived = FrameUtil.Receive(socket);                
                string response = "EXCEEDED";
                byte[] data = Encoding.ASCII.GetBytes(response);
                Frame frame = new Frame(CMD.Connect, data);
                FrameUtil.Send(socket, frame);
                Frame frameExit = new Frame(CMD.Exit, Encoding.ASCII.GetBytes(""));
                FrameUtil.Send(clientNotify, frameExit);
            }
            else
            {
                
                while (!isSocketClosed)
                {

                    try
                    {
                        frameReceived = FrameUtil.Receive(socket);
                    }
                    catch (SocketException e)
                    {
                        isSocketClosed = true;
                    }
                    if (frameReceived == null)
                    {
                        isSocketClosed = true;
                    }
                    else
                    {
                        try
                        {
                            switch (frameReceived.CMD)
                            {
                                case CMD.Connect:
                                    string informationRecevived = Encoding.ASCII.GetString(frameReceived.Data, 0, frameReceived.DataLength);
                                    user = informationRecevived.Split('@')[0];
                                    int portNotify = int.Parse(informationRecevived.Split('@')[1]);
                                    string ipNotify = informationRecevived.Split('@')[2];
                                    string response = ServerRequest.Connect(frameReceived, lists.GetUsers());
                                    if (response.Equals("OK"))
                                    {
                                        AddUserInList(user, clientNotify);
                                        clientCount++;
                                    }
                                    byte[] data = Encoding.ASCII.GetBytes(response);
                                    Frame frame = new Frame(CMD.Connect, data);
                                    FrameUtil.Send(socket, frame);
                                    break;
                                case CMD.ListFiles:
                                    ServerRequest.ListFiles(socket, lists.GetFiles(), route);
                                    break;
                                case CMD.ListUsers:
                                    ServerRequest.ListUsers(socket, lists.GetUsers());
                                    break;
                                case CMD.UploadFile:
                                    FileTransfer file = ServerRequest.UploadFile(socket);
                                    lists.AddFile(file);
                                    break;
                                case CMD.DownloadFile:
                                    string fileToDownLoad = Encoding.ASCII.GetString(frameReceived.Data, 0, frameReceived.DataLength);
                                    ServerRequest.DownloadFile(socket, fileToDownLoad, lists);
                                    break;
                                case CMD.Notify:
                                    string responseNotify = Encoding.ASCII.GetString(frameReceived.Data, 0, frameReceived.DataLength);
                                    string fileName = responseNotify.Split('@')[0];
                                    string userNotify = responseNotify.Split('@')[1];
                                    string userOrigin = responseNotify.Split('@')[2];
                                    ServerRequest.Notify(socket, userNotify, userOrigin, fileName, lists);
                                    break;
                                case CMD.Exit:
                                    ServerRequest.Exit(user, lists);
                                    user = Encoding.ASCII.GetString(frameReceived.Data, 0, frameReceived.DataLength);
                                    RemoveUser(user);
                                    isSocketClosed = true;
                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Ha sucedido un error inesperado");

                            if(!user.Equals(""))
                                RemoveUser(user);
                            
                            isSocketClosed = true;
                        }
                    }
                }
            }
            //    socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            
        }
        
        private static void DeleteFile(string route)
        {
            if (Directory.Exists(route))
            {
                Directory.Delete(route, true);
            }
        }

        private static void CreateFolderServer(string route)
        {
            bool folderExists = Directory.Exists(route);
            if (!folderExists)
            {
                Directory.CreateDirectory(route);
            }
        }

        private static void RemoveUser(string userName)
        {
            User user = lists.GetUserByName(userName);
            if (user != null)
            {
                lists.RemoveUser(user);
                clientCount--;
            }
            else
            {
                Console.WriteLine("Error!, No se ha podido eliminar el user: " + userName);
            }
        }

        private static void AddUserInList(string userName, Socket socketNotify)
        {
            User user = new User(userName, socketNotify);
            lists.AddUser(user);
        }
    }
}

