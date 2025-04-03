using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace MonopConsole.Data;

public class DbService : IDbService
{
    private readonly IDbConnection _db;
    string connectionString = "Data Source=monop.db";

    public DbService()
    {
        _db = new SqliteConnection(connectionString);
    }

    public Task<T> GetAsync<T>(string command, object parms) =>
        _db.QueryFirstAsync<T>(command, parms);

    public  Task<IEnumerable<T>> GetAll<T>(string command, object parms) =>
         _db.QueryAsync<T>(command, parms);

    public Task BulkInsert<T>(List<T> data) => _db.InsertAsync(data);
    public async Task<int> EditData(string command, object parms) => await _db.ExecuteAsync(command, parms);
}
