using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Entity
{
    public class Repository
    {

        private static List<User> users;
        private static List<FileTransfer> files;

        public Repository()
        {
            users = new List<User>();
            files = new List<FileTransfer>();
        }

        public void AddUser(User user)
        {
            users.Add(user);
        }

        public void AddFile(FileTransfer fileTransfer)
        {
            files.Add(fileTransfer);
        }

        public List<FileTransfer> GetFiles()
        {
            return files;
        }

        public List<User> GetUsers()
        {
            return users;
        }

        public void RemoveUser(User user)
        {
            users.Remove(user);
        }

        public void RemoveFile(string file)
        {
            FileTransfer auxFile = this.GetFilesByName(file);
            files.Remove(auxFile);
        }

        public int GetCountUsers()
        {
            return users.Count;
        }

        public User GetUserByName(string userName)
        {
            User userAux = new User(userName,null);
            foreach (var user in users)
            {
                if (user.Equals(userAux))
                {
                    return user;
                }
            }
            return null;
        }

        public FileTransfer GetFilesByName(string fileName)
        {
            FileTransfer fileAux = new FileTransfer(fileName);
            foreach (var file in files)
            {
                if (file.Equals(fileAux))
                {
                    return fileAux;
                }
            }
            return null;
        }
    }
}
