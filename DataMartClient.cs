using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DeveloperJosephBittner.DataMart
{
    public static class DataMartClient
    {
        public sealed class ShipmentListItem
        {
            public string ShipmentKey { get; set; } = string.Empty;
            public DateTime? LoadDate { get; set; }
            public string? LoadDateRaw { get; set; }
            public string? LoadDateSource { get; set; }
            public string? TimeOfDay { get; set; }
            public decimal QuantityShipped { get; set; }
            public decimal QuantityOrdered { get; set; }
            public decimal PricePerUnit { get; set; }
            public decimal TotalCostShipped { get; set; }
            public decimal TotalShipNetWeight { get; set; }
            public string? ProductType { get; set; }
            public string? ItemCategoryGroup { get; set; }
            public string? LoadDoor { get; set; }
            public string? LoadPicker { get; set; }
            public decimal? Stacks { get; set; }
            public string? TotalStacks { get; set; }
            public string? Description { get; set; }
        }

        public sealed class ShipmentDetail
        {
            public string ShipmentKey { get; set; } = string.Empty;
            public string ItemNumber { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal QuantityShipped { get; set; }
            public decimal QuantityOrdered { get; set; }
            public decimal PricePerUnit { get; set; }
            public decimal TotalCostShipped { get; set; }
            public decimal TotalShipNetWeight { get; set; }
            public string? ProductType { get; set; }
            public string? ItemCategoryGroup { get; set; }
            public DateTime? LoadDate { get; set; }
            public string? LoadDateRaw { get; set; }
            public string? LoadDateSource { get; set; }
            public string? TimeOfDay { get; set; }
            public string? LoadDoor { get; set; }
            public string? LoadPicker { get; set; }
            public decimal? Stacks { get; set; }
            public string? TotalStacks { get; set; }
        }

        public sealed class ShipCountSummary
        {
            public string ItemNumber { get; set; } = string.Empty;
            public int ShipmentCount { get; set; }
            public int ShipmentsWithLoadDate { get; set; }
            public decimal TotalQuantityShipped { get; set; }
            public decimal TotalCostShipped { get; set; }
        }

        public sealed class ShipCountResult
        {
            public string ItemNumber { get; set; } = string.Empty;
            public List<ShipmentListItem> Shipments { get; set; } = new();
            public ShipmentDetail? SelectedShipment { get; set; }
            public ShipCountSummary Summary { get; set; } = new();
        }

        /// <summary>
        /// Retrieves the first N user tables from the database and returns them as a list.
        /// </summary>
        public static async Task<List<(string Schema, string Name)>> GetTopTablesAsync(string connectionString, int top = 20)
        {
            const string sql = @"SELECT TOP (@top)
                    s.name AS SchemaName,
                    t.name AS TableName
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                ORDER BY s.name, t.name";

            var tables = new List<(string Schema, string Name)>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@top", top);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }

            return tables;
        }

        /// <summary>
        /// Retrieves the first N user tables from the database and prints them to the console.
        /// </summary>
        public static async Task PrintTopTablesAsync(string connectionString, int top = 20)
        {
            var tables = await GetTopTablesAsync(connectionString, top);

            Console.WriteLine($"Found {tables.Count} tables (top {top}):");
            foreach (var (schema, name) in tables)
            {
                Console.WriteLine($"  {schema}.{name}");
            }
        }

        /// <summary>
        /// Creates a SQL Server connection string targeting the configured server and database.
        /// </summary>
        public static string BuildConnectionString(
            string server = "vFS-SQL02-NS",
            string database = "DeveloperJosephBittner",
            bool trustedConnection = true,
            bool trustServerCertificate = true)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = trustedConnection,
                TrustServerCertificate = trustServerCertificate
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Retrieves shipment rows for an item and optional selected shipment detail.
        /// Data is sourced from dbo.DETAL1 and keyed to dbo.STAGE for load date/time context.
        /// </summary>
        public static async Task<ShipCountResult> GetShipmentsByItemAsync(
            string connectionString,
            string itemNumber,
            string? selectedShipmentKey)
        {
            // Match priority for STAGE-derived fields:
            // 1) Exact shipment key, 2) Order-number key pattern, 3) Ship date fallback.
            // HEDER1 is used as a final fallback for fields not reliably present in STAGE.
            const string sql = @"
SELECT
    LTRIM(RTRIM(d.[KEY])) AS ShipmentKey,
    d.ITEMNUM,
    d.DESCRIPTION,
    ISNULL(d.QTYSHIP, 0) AS QuantityShipped,
    ISNULL(d.QTYORD, 0) AS QuantityOrdered,
    ISNULL(d.PRICEPERUNIT, 0) AS PricePerUnit,
    ISNULL(d.TOTSHIPNETWT, 0) AS TotalShipNetWeight,
    d.PRODTYPE,
    d.ITEMCATGRP,
    COALESCE(
        TRY_CONVERT(date, LEFT(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), 10), 101),
        hParsed.ShipDateParsed
    ) AS LoadDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), ''),
        NULLIF(LTRIM(RTRIM(h.SHIPDATE)), '')
    ) AS LoadDateRaw,
    NULLIF(LTRIM(RTRIM(COALESCE(sExact.TIMEOFDAY, sFallback.TIMEOFDAY, sDateFallback.TIMEOFDAY))), '') AS TimeOfDay,
    NULLIF(LTRIM(RTRIM(COALESCE(sExact.LOADDOOR, sFallback.LOADDOOR, sDateFallback.LOADDOOR))), '') AS LoadDoor,
    COALESCE(
        NULLIF(LTRIM(RTRIM(COALESCE(sExact.LOADPICKER, sFallback.LOADPICKER, sDateFallback.LOADPICKER))), ''),
        NULLIF(LTRIM(RTRIM(h.PICKERIDNUM)), '')
    ) AS LoadPicker,
    COALESCE(
        TRY_CONVERT(decimal(18,2), NULLIF(LTRIM(RTRIM(CONVERT(varchar(50), COALESCE(sExact.STACKS, sFallback.STACKS, sDateFallback.STACKS)))), '')),
        TRY_CONVERT(decimal(18,2), NULLIF(LTRIM(RTRIM(CONVERT(varchar(50), h.STACKS))), ''))
    ) AS Stacks,
    COALESCE(
        NULLIF(LTRIM(RTRIM(COALESCE(sExact.TOTSTACKS, sFallback.TOTSTACKS, sDateFallback.TOTSTACKS))), ''),
        NULLIF(LTRIM(RTRIM(CONVERT(varchar(50), h.STACKS))), '')
    ) AS TotalStacks,
    CASE
        WHEN NULLIF(LTRIM(RTRIM(sExact.[KEY])), '') IS NOT NULL THEN 'STAGE (Exact Key)'
        WHEN NULLIF(LTRIM(RTRIM(sFallback.[KEY])), '') IS NOT NULL THEN 'STAGE (Order Match)'
        WHEN NULLIF(LTRIM(RTRIM(sDateFallback.[KEY])), '') IS NOT NULL THEN 'STAGE (Date Match)'
        WHEN NULLIF(LTRIM(RTRIM(h.SHIPDATE)), '') IS NOT NULL THEN 'HEDER1'
        ELSE NULL
    END AS LoadDateSource
FROM dbo.DETAL1 d
LEFT JOIN dbo.HEDER1 h
    ON h.ORDERNUM = LEFT(LTRIM(RTRIM(d.[KEY])), 6)
OUTER APPLY (
    SELECT
        CASE
            WHEN LEN(LTRIM(RTRIM(h.SHIPDATE))) = 6
                 AND LTRIM(RTRIM(h.SHIPDATE)) NOT LIKE '%[^0-9]%'
                THEN TRY_CONVERT(date, STUFF(STUFF(LTRIM(RTRIM(h.SHIPDATE)), 3, 0, '/'), 6, 0, '/'), 1)
            WHEN LEN(LTRIM(RTRIM(h.SHIPDATE))) = 8
                 AND LTRIM(RTRIM(h.SHIPDATE)) NOT LIKE '%[^0-9]%'
                THEN TRY_CONVERT(date, STUFF(STUFF(LTRIM(RTRIM(h.SHIPDATE)), 3, 0, '/'), 6, 0, '/'), 101)
            ELSE TRY_CONVERT(date, LTRIM(RTRIM(h.SHIPDATE)), 101)
        END AS ShipDateParsed
) hParsed
OUTER APPLY (
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS,
        s.TOTSTACKS
    FROM dbo.STAGE s
    WHERE LTRIM(RTRIM(s.[KEY])) = LTRIM(RTRIM(d.[KEY]))
) sExact
OUTER APPLY (
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS,
        s.TOTSTACKS
    FROM dbo.STAGE s
    WHERE LTRIM(RTRIM(s.[KEY])) LIKE '%' + LEFT(LTRIM(RTRIM(d.[KEY])), 6) + '%'
    ORDER BY
        CASE WHEN NULLIF(LTRIM(RTRIM(s.LOADDATE)), '') IS NULL THEN 1 ELSE 0 END,
        LTRIM(RTRIM(s.[KEY])) DESC
) sFallback
OUTER APPLY (
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS,
        s.TOTSTACKS
    FROM dbo.STAGE s
    WHERE TRY_CONVERT(date, LEFT(LTRIM(RTRIM(s.LOADDATE)), 10), 101) = hParsed.ShipDateParsed
    ORDER BY
        CASE WHEN NULLIF(LTRIM(RTRIM(s.LOADDATE)), '') IS NULL THEN 1 ELSE 0 END,
        LTRIM(RTRIM(s.[KEY])) DESC
) sDateFallback
WHERE d.ITEMNUM = @itemNumber
ORDER BY
    CASE WHEN COALESCE(
        TRY_CONVERT(date, LEFT(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), 10), 101),
        hParsed.ShipDateParsed
    ) IS NULL THEN 1 ELSE 0 END,
    COALESCE(
        TRY_CONVERT(date, LEFT(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), 10), 101),
        hParsed.ShipDateParsed
    ) DESC,
    LTRIM(RTRIM(d.[KEY])) DESC;";

            // Initialize the response object before reading any rows.
            var result = new ShipCountResult
            {
                ItemNumber = itemNumber,
                Summary = new ShipCountSummary
                {
                    ItemNumber = itemNumber
                }
            };

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@itemNumber", itemNumber);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Build one shipment list row from the current record.
                var shipmentKey = reader.GetString(0);
                var loadDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
                var quantityShipped = reader.GetDecimal(3);
                var pricePerUnit = reader.GetDecimal(5);
                var shipment = new ShipmentListItem
                {
                    ShipmentKey = shipmentKey,
                    LoadDate = loadDate,
                    LoadDateRaw = reader.IsDBNull(10) ? null : reader.GetString(10),
                    LoadDateSource = reader.IsDBNull(16) ? null : reader.GetString(16),
                    TimeOfDay = reader.IsDBNull(11) ? null : reader.GetString(11),
                    QuantityShipped = quantityShipped,
                    QuantityOrdered = reader.GetDecimal(4),
                    PricePerUnit = pricePerUnit,
                    TotalCostShipped = Math.Round(quantityShipped * pricePerUnit, 2),
                    TotalShipNetWeight = reader.GetDecimal(6),
                    ProductType = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ItemCategoryGroup = reader.IsDBNull(8) ? null : reader.GetString(8),
                    LoadDoor = reader.IsDBNull(12) ? null : reader.GetString(12),
                    LoadPicker = reader.IsDBNull(13) ? null : reader.GetString(13),
                    Stacks = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                    TotalStacks = reader.IsDBNull(15) ? null : reader.GetString(15),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                };

                result.Shipments.Add(shipment);

                // If a specific shipment was requested, capture its expanded detail payload.
                if (!string.IsNullOrWhiteSpace(selectedShipmentKey)
                    && string.Equals(shipmentKey, selectedShipmentKey.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    result.SelectedShipment = new ShipmentDetail
                    {
                        ShipmentKey = shipmentKey,
                        ItemNumber = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        QuantityShipped = quantityShipped,
                        QuantityOrdered = reader.GetDecimal(4),
                        PricePerUnit = pricePerUnit,
                        TotalCostShipped = Math.Round(quantityShipped * pricePerUnit, 2),
                        TotalShipNetWeight = reader.GetDecimal(6),
                        ProductType = reader.IsDBNull(7) ? null : reader.GetString(7),
                        ItemCategoryGroup = reader.IsDBNull(8) ? null : reader.GetString(8),
                        LoadDate = loadDate,
                        LoadDateRaw = reader.IsDBNull(10) ? null : reader.GetString(10),
                        LoadDateSource = reader.IsDBNull(16) ? null : reader.GetString(16),
                        TimeOfDay = reader.IsDBNull(11) ? null : reader.GetString(11),
                        LoadDoor = reader.IsDBNull(12) ? null : reader.GetString(12),
                        LoadPicker = reader.IsDBNull(13) ? null : reader.GetString(13),
                        Stacks = reader.IsDBNull(14) ? (decimal?)null : reader.GetDecimal(14),
                        TotalStacks = reader.IsDBNull(15) ? null : reader.GetString(15)
                    };
                }
            }

            // Compute summary metrics from the returned shipment rows.
            var total = 0m;
            var withDate = 0;
            var totalCost = 0m;
            foreach (var shipment in result.Shipments)
            {
                total += shipment.QuantityShipped;
                totalCost += shipment.TotalCostShipped;
                if (shipment.LoadDate.HasValue)
                {
                    withDate++;
                }
            }

            result.Summary = new ShipCountSummary
            {
                ItemNumber = itemNumber,
                ShipmentCount = result.Shipments.Count,
                ShipmentsWithLoadDate = withDate,
                TotalQuantityShipped = total,
                TotalCostShipped = Math.Round(totalCost, 2)
            };

            return result;
        }
    }
}
