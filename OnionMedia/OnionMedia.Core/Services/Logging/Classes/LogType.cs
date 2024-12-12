using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnionMedia.Core.Services.Logging.Classes
{
    public class LogType
    {
        public string LogMessage { get; set; }
        public string LogTime { get; set; }
        public LogLevel LogPriority { get;set; }
    }
}
