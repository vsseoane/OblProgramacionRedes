using Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Entity;
using System.IO;
using System.Configuration;
using System.Net;

namespace Server
{
    public class ServerRequest
    {
        public static string Connect(Frame frame, List<User> users)
        {
            string receive = Encoding.ASCII.GetString(frame.Data, 0, frame.DataLength);
            string nameUser = receive.Split('@')[0];
            User userAux = new User(nameUser, null);
            foreach (var user in users)
            {
                if (user != null && user.Equals(userAux))
                {
                    return "REPEAT";
                }
            }
            return "OK";
        }

        public static void ListFiles(Socket socket, List<FileTransfer> files, string route)
        {
            string response = "";
            string routeFull = "";
            if (files != null && files.Count != 0)
            {
                foreach (var file in files)
                {
                    routeFull = route + file.Name;
                    if (file.Name != null && File.Exists(routeFull))
                    {
                        response += file.Name + ";";
                    }
                }
            }
            Frame frame = new Frame(CMD.ListFiles, Encoding.ASCII.GetBytes(response));
            FrameUtil.Send(socket, frame);

        }

        public static void ListUsers(Socket socket, List<User> users)
        {
            string response = "";
            foreach (var user in users)
            {
                if (user != null)
                {
                    response += user.Name + ";";
                }
            }

            Frame frame = new Frame(CMD.ListUsers, Encoding.ASCII.GetBytes(response));
            FrameUtil.Send(socket, frame);
        }

        public static FileTransfer UploadFile(Socket socket)
        {
            string route = ConfigurationManager.AppSettings["Route"].ToString();
            FileTransfer file = FrameUtil.ReceiveFile(socket, route);

            return file;

        }

        public static void DownloadFile(Socket socket, string fileName, Repository lists)
        {
            string response = "";
            Frame frame;
            bool IsError = false;
            string route = ConfigurationManager.AppSettings["Route"].ToString();
            string routeFull = route + fileName;
            bool existFile = File.Exists(routeFull);
            if (lists.GetFilesByName(fileName) != null)
            {
                if (existFile)
                {
                    response = "OK";
                    frame = new Frame(CMD.DownloadFile, Encoding.ASCII.GetBytes(response));
                    FrameUtil.Send(socket, frame);
                    FrameUtil.SendFile(socket, routeFull);
                }
                else
                {
                    lists.RemoveFile(fileName);
                    IsError = true;
                }


            }
            else
            {
                IsError = true;
            }
            if (IsError)
            {
                response = "ERROR";
                frame = new Frame(CMD.DownloadFile, Encoding.ASCII.GetBytes(response));
                FrameUtil.Send(socket, frame);
            }

        }

        public static void Notify(Socket socket, string userNotify, string userOrigin, string fileName, Repository lists)
        {
            string response = "";
            Frame frame;
            if (lists.GetFilesByName(fileName) != null)
            {
                if (userNotify.Equals(""))
                {
                    if (!IsUserListEmpty(lists))
                    {
                        NotifyAll(lists, userOrigin, fileName);
                        response = "OKALL";
                        frame = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(response));
                        FrameUtil.Send(socket, frame);
                    }
                    else
                    {
                        response = "USERLISTEMPTY";
                        frame = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(response));
                        FrameUtil.Send(socket, frame);
                    }

                }
                else
                {
                    if (lists.GetUserByName(userNotify) != null)
                    {
                        NotifyUser(userNotify, userOrigin, lists, fileName);
                        response = "OKUSER";
                        frame = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(response));
                        FrameUtil.Send(socket, frame);
                    }
                    else
                    {
                        response = "USERNOTEXIST";
                        frame = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(response));
                        FrameUtil.Send(socket, frame);
                    }
                }
            }
            else
            {
                response = "FILENOTEXIST";
                frame = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(response));
                FrameUtil.Send(socket, frame);
            }
        }

        public static void Exit(string user, Repository lists)
        {
            User userToClose = lists.GetUserByName(user);
            Frame frame = new Frame(CMD.Exit, Encoding.ASCII.GetBytes(""));
            FrameUtil.Send(userToClose.SocketNotify, frame);
        }

        private static bool IsUserListEmpty(Repository lists)
        {
            return lists.GetCountUsers() <= 1;
        }

        private static void NotifyUser(string user, string userOrigin, Repository list, string fileName)
        {
            User userNotify = list.GetUserByName(user);
            string messageToNotify = "El usuario " + userOrigin + " te notifica que el archivo " + fileName + " ha sido subido.";
            Frame frame = new Frame(CMD.Notify, Encoding.ASCII.GetBytes(messageToNotify));
            Socket socketToNotify = userNotify.SocketNotify;
            FrameUtil.Send(socketToNotify, frame);

        }

        private static void NotifyAll(Repository lists, string userOrigin, string fileName)
        {
            List<User> users = lists.GetUsers();
            foreach (var user in users)
            {
                NotifyUser(user.Name, userOrigin, lists, fileName);
            }
        }
    }
}
