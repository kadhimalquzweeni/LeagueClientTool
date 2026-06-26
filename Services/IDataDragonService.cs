namespace LoLClientTool.Services
{
    public interface IDataDragonService
    {
        Task<string> GetLatestVersionAsync();
    }
}