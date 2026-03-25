using System;
using System.IO;
using Microsoft.Data.Sqlite;

var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var dbPath = Path.Combine(appData, "enBot", "lingua.db");

// Copy to temp to avoid file-lock issues while enBot is running
var tempPath = Path.Combine(Path.GetTempPath(), "enBot_query.db");
File.Copy(dbPath, tempPath, overwrite: true);
if (File.Exists(dbPath + "-wal")) File.Copy(dbPath + "-wal", tempPath + "-wal", overwrite: true);
if (File.Exists(dbPath + "-shm")) File.Copy(dbPath + "-shm", tempPath + "-shm", overwrite: true);

var query = args.Length > 0
    ? string.Join(" ", args)
    : "SELECT Id, Score, Complexity, ReceivedAt, Original FROM PromptEntries ORDER BY Id DESC LIMIT 10";

using var conn = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = query;
using var r = cmd.ExecuteReader();

for (int i = 0; i < r.FieldCount; i++)
    Console.Write($"{r.GetName(i),-25}");
Console.WriteLine();
Console.WriteLine(new string('-', r.FieldCount * 25));

while (r.Read())
{
    for (int i = 0; i < r.FieldCount; i++)
    {
        var val = r.IsDBNull(i) ? "NULL" : r.GetValue(i).ToString()!;
        if (val.Length > 24) val = val[..21] + "...";
        Console.Write($"{val,-25}");
    }
    Console.WriteLine();
}
