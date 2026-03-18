#r "nuget: Microsoft.Data.SqlClient, 5.1.1"
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

var connStr = "Server=localhost,1433;Database=InventoryDB;User Id=sa;Password=Bb42461503;Encrypt=False;TrustServerCertificate=True;";
using var conn = new SqlConnection(connStr);
conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = "UPDATE JigSpecs SET Week = '1/11', Item = '1', Rev = '2ND', ToyNumber = 'GJG82-P000A', PartType = 'BODY', ToolType = 'NEW', Feed = '-140', Scan = '-69' WHERE ToolNo = 'GBV32';";
int rows = cmd.ExecuteNonQuery();
Console.WriteLine($"Updated {rows} rows in JigSpecs");
