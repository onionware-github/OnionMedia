using OnionMedia.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Services.Store;
using Windows.System;

namespace OnionMedia.Services;

sealed class SoftwareUpdateService : ISoftwareUpdateService
{
	private StoreContext context;

	public async Task<bool> CheckForUpdateAsync()
	{
#if DEBUG
		return false;
#endif
		context ??= StoreContext.GetDefault();

		// Get the updates that are available.
		var updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();

		return updates?.Count > 0;
	}

	public async Task StartUpdateProcessAsync()
	{
		await Launcher.LaunchUriAsync(new("ms-windows-store://pdp/?productid=9n252njjqb65"));
	}
}
