using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQListenerLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            FileTextLogger fileLogger = new FileTextLogger();
            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                try
                {
                    MessageQueue msgQ = new MessageQueue(".\\Private$\\scdlogs");
                    Log mylog = new Log();
                    Object o = new Object();
                    System.Type[] arrTypes = new System.Type[2];
                    arrTypes[0] = mylog.GetType();
                    arrTypes[1] = o.GetType();
                    msgQ.Formatter = new XmlMessageFormatter(arrTypes);
                    mylog = ((Log)msgQ.Receive().Body);
                    fileLogger.log(mylog.action, mylog.date, mylog.message);
                }
                catch (Exception)
                {

                }
            }
            return 0;
        }
    }
}
