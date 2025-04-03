namespace MonopConsole.Data;

public interface IDbService
{
    Task<T> GetAsync<T>(string command, object parms);
    Task<IEnumerable<T>> GetAll<T>(string command, object parms);
    Task BulkInsert<T>(List<T> data);
    Task<int> EditData(string command, object parms);
}
