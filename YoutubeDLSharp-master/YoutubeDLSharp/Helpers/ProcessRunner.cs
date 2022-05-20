using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp.Helpers
{
    /// <summary>
    /// Provides methods for throttled execution of processes.
    /// </summary>
    public class ProcessRunner
    {
        private const int MAX_COUNT = 100;
        private SemaphoreSlim semaphore;

        public byte TotalCount { get; private set; }

        public ProcessRunner(byte initialCount)
        {
            semaphore = new SemaphoreSlim(initialCount, MAX_COUNT);
            TotalCount = initialCount;
        }

        public async Task<(int, string[])> RunThrottled(YoutubeDLProcess process, string[] urls, OptionSet options,
                                       CancellationToken ct, IProgress<DownloadProgress> progress = null)
        {
            var errors = new List<string>();
            process.ErrorReceived += (o, e) => errors.Add(e.Data);
            await semaphore.WaitAsync(ct);
            try
            {
                var exitCode = await process.RunAsync(urls, options, ct, progress);
                return (exitCode, errors.ToArray());
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void incrementCount(byte incr)
        {
            semaphore.Release(incr);
            TotalCount += incr;
        }

        private async Task decrementCount(byte decr)
        {
            Task[] decrs = new Task[decr];
            for (int i = 0; i < decr; i++)
                decrs[i] = semaphore.WaitAsync();
            TotalCount -= decr;
            await Task.WhenAll(decrs);
        }

        public async Task SetTotalCount(byte count)
        {
            if (count < 1 || count > MAX_COUNT)
                throw new ArgumentException($"Number of threads must be between 1 and {MAX_COUNT}.");
            if (count > TotalCount)
                incrementCount((byte)(count - TotalCount));
            else if (count < TotalCount)
                await decrementCount((byte)(TotalCount - count));
        }
    }
}
