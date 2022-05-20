
namespace YoutubeDLSharp
{
    /// <summary>
    /// Encapsulates the output of a youtube-dl download operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RunResult<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; }
        /// <summary>
        /// The accumulated error output.
        /// </summary>
        public string[] ErrorOutput { get; }
        /// <summary>
        /// The output data.
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// Creates a new instance of class RunResult.
        /// </summary>
        public RunResult(bool success, string[] error, T result)
        {
            this.Success = success;
            this.ErrorOutput = error;
            this.Data = result;
        }
    }
}
