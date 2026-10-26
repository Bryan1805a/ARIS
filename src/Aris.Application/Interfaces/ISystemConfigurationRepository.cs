namespace Aris.Application.Interfaces;

public interface ISystemConfigurationRepository
{
    Task<string?> GetValueAsync(string configKey);
}