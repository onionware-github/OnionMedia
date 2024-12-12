using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnionMedia.Core.Services.Logging.Classes;
using OnionMedia.Core.Services.Logging.Interfaces;

namespace OnionMedia.Core.Services.Logging
{
    public class CentralLoggerService : ICentralLoggerService
    {
        private readonly ConcurrentQueue<LogType> _logQueue = new ConcurrentQueue<LogType>();
        private readonly ILogger<CentralLoggerService> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CentralLoggerService(ILogger<CentralLoggerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            StartLogging();
        }

        public void StartLogging()
        {
            Task.Run(() => ProcessLogsAsync(_cancellationTokenSource.Token));
        }


        public void EnqueueLog(LogType log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            _logQueue.Enqueue(log);
        }

        private async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_logQueue.TryDequeue(out LogType log))
                {
                    _logger.Log(log.LogPriority, "{Message} at {Time}", log.LogMessage, log.LogTime);
                    Debug.WriteLine((log.LogPriority, log.LogMessage, log.LogTime));
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        public void StopLogging()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
