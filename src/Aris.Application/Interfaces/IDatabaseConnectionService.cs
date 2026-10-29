namespace Aris.Application.Interfaces;

public interface IDatabaseConnectionService
{
    Task<bool> CheckConnectionAsync();
}