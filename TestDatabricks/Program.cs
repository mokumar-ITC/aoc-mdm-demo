using System;
using System.Data;
using System.Data.Odbc;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DatabricksConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
           // string connectionString = "Driver=aoc_sandbox;" +
                                       //"Host=adb-2721506690757516.16.azuredatabricks.net;" +
                                       //"Port=443;" +
                                       //"HTTPPath=/sql/1.0/warehouses/b949f4dc91b0e151;" +
                                       //"UID=token;" +
                                       //"PWD=<DATABRICKS_TOKEN>;"; // Replace with your token

            //string connectionString = "Driver = myDatabricks; Host = adb - 2721506690757516.16.azuredatabricks.net; Port = 443; HTTPPath =/ sql / 1.0 / warehouses / b949f4dc91b0e151; UID = token; PWD = <DATABRICKS_TOKEN>;SSL=1; ";
            string dsn = "dsn=aoc_sandbox";
            using OdbcConnection connection = new OdbcConnection(dsn);
            try
            {
                // Open the connection
                connection.Open();
                Console.WriteLine("Connected to Azure Databricks!");

                // Create a command to execute a SQL query
                string query = "select * from mdm_sandbox.mdm.aoc_level_3"; // Replace with your table name
                using OdbcCommand command = new OdbcCommand(query, connection);
                using OdbcDataReader reader = command.ExecuteReader(); // Use synchronous method

                // Read and display the results
                while (await reader.ReadAsync())
                {
                    // Assuming your_table_name has columns "id" and "name"
                    Console.WriteLine($"ID: {reader["id"]}, Name: {reader["name"]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure the connection is closed
                await connection.CloseAsync();
                Console.WriteLine("Connection closed.");
            }
        }
    }
}