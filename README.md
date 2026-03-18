# DeveloperJosephBittner Data Mart

This is an ASP.NET Core MVC web application that provides a user-friendly interface for interacting with SQL Server databases. The application features a modern, responsive design built with **Tailwind CSS** and connects to the `DeveloperJosephBittner` database on the `SQL` server.

## How to run

1. Open a terminal in the project folder:
   ```powershell
   cd C:\Users\joseph.bittner\source\DataMart
   ```

2. (Optional) Override the connection string via an environment variable:
   ```powershell
   $env:DATA_MART_CONNECTION_STRING = "Server=SQL Server;Database=DeveloperJosephBittner;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

3. Run the app:
 PS C:\Program Files\dotnet> .\dotnet.exe run --project C:\Users\joseph.bittner\source\DataMart\DeveloperJosephBittner.DataMart.csproj
 

4. Open a web browser and navigate to `https://localhost:5001` (or the URL shown in the console).

The application will display a responsive web page with "Joe's Data Mart" header and a button to test the database connection. The interface is built with Tailwind CSS for modern, responsive design.

## What it does

The app connects to the database and prints the first 20 user tables (schema.table) found in `sys.tables`.

## Extending the data mart

- Add additional query methods to `DataMartClient.cs`.
- Add a configuration layer (e.g., `appsettings.json`) if you need more runtime configuration.
