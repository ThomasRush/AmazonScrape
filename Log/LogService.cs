using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    public static class LogService
    {
        public static ILog GetLogService(string name)
        {
            // Check the calling name and return
            // the appropriate log object
            return new Log(name);
        }
    }
}
