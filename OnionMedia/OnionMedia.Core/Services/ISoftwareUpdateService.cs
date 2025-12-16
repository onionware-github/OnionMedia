namespace OnionMedia.Core.Services;

public interface ISoftwareUpdateService
{
	/// <summary>
	/// Checks for updates.
	/// </summary>
	/// <returns>True, when updates are available, false if not.</returns>
	Task<bool> CheckForUpdateAsync();
	Task StartUpdateProcessAsync();
}
