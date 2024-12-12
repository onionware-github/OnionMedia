using OnionMedia.Core.Services.Logging.Classes;
using System.Threading.Tasks;

namespace OnionMedia.Core.Services.Logging.Interfaces
{
    public interface ICentralLoggerService
    {
        void EnqueueLog(LogType log);

        void StopLogging();
    }
}
