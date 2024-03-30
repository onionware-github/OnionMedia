namespace OnionMedia.Core.Services;

public interface IPCPower
{
	void Shutdown();
	void Standby();
	void Hibernate();
}
