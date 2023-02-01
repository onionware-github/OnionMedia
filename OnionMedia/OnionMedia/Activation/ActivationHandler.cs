using System.Threading.Tasks;

namespace OnionMedia.Activation
{
    // For more information on understanding activation flow see
    // https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/WinUI/activation.md
    //
    // Extend this class to implement new ActivationHandlers
    public abstract class ActivationHandler<T> : IActivationHandler
    {
        // Override this method to add the activation logic in your activation handler
        protected abstract Task HandleInternalAsync(T args);

        public async Task HandleAsync(object args)
        {
            await HandleInternalAsync((T)args);
        }

        public bool CanHandle(object args)
        {
            // CanHandle checks the args is of type you have configured
            return args is T && CanHandleInternal((T)args);
        }

        // You can override this method to add extra validation on activation args
        // to determine if your ActivationHandler should handle this activation args
        protected virtual bool CanHandleInternal(T args)
        {
            return true;
        }
    }
}
