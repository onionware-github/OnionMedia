using Microsoft.Extensions.Logging;
using OnionMedia.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnionMedia.Core.Classes
{
    public class LoggerClass
    {
        private readonly ILogger<LoggerClass> _logger;

        public LoggerClass(ILogger<LoggerClass> logger)
        {
            _logger = logger;
        }
    }
}

