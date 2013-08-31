using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    public class Log : ILog
    {
        private string _name;

        public Log(string name)
        {
            _name = name;
        }

        public void Output(string text)
        {            
            long ticks = DateTime.Now.Ticks;
            string result = string.Format("Log {0} @ {1}: {2}", ticks, _name, text);
            Console.WriteLine(result);
        }
        
    }
}
