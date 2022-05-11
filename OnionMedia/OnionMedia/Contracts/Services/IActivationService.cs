using System.Threading.Tasks;

namespace OnionMedia.Contracts.Services
{
    public interface IActivationService
    {
        Task ActivateAsync(object activationArgs);
    }
}
