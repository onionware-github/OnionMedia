using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OnionMedia.Core.Services.Implementations;

public sealed class WindowsPowerService : IPCPower
{
	[DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

	public void Shutdown()
	{
		Process.Start("shutdown", "/s /t 0");
	}

	public void Standby()
	{
		SetSuspendState(false, true, true);
	}

	public void Hibernate()
	{
		SetSuspendState(true, true, true);
	}
}