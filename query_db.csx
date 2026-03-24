#r "nuget: Microsoft.Data.SqlClient, 5.1.1"
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

var connStr = "Server=.\\SQLEXPRESS;Database=InventoryDB;User Id=James;Password=Bb424615;Encrypt=True;TrustServerCertificate=True;";
using var conn = new SqlConnection(connStr);
conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT Id, Name, Type FROM Locators WHERE Id LIKE '%MBK1%' OR Type = 'Store'";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader["Id"]} | {reader["Name"]} | {reader["Type"]}");
}
