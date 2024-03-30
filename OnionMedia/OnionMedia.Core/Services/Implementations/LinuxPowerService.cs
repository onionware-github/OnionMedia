using System.Diagnostics;

namespace OnionMedia.Core.Services.Implementations;

public sealed class LinuxPowerService : IPCPower
{
	public void Shutdown()
	{
		Process.Start("shutdown", "-h now");
	}

	public void Standby()
	{
		Process.Start("systemctl", "suspend");
	}

	public void Hibernate()
	{
		Process.Start("systemctl", "hibernate");
	}
}
