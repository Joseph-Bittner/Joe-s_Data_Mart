using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace DeveloperJosephBittner.DataMart
{
    public static class DataMartClient
    {
        public sealed class ShipmentListItem
        {
            public string ShipmentKey { get; set; } = string.Empty;
            // Normalized key used for robust matching across tables
            public string NormalizedKey { get; set; } = string.Empty;
            // JSON payload containing the raw row values (redacted/truncated as needed)
            public string? RawDataJson { get; set; }
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
            public string? Description { get; set; }
        }

        public sealed class ShipmentDetail
        {
            public string ShipmentKey { get; set; } = string.Empty;
            // Normalized key used for robust matching across tables
            public string NormalizedKey { get; set; } = string.Empty;
            // JSON payload containing the raw row values (redacted/truncated as needed)
            public string? RawDataJson { get; set; }
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
        /// Executes a lightweight query and returns sample TimeOfDay values.
        /// </summary>
        public static async Task<List<string>> GetSampleTimeOfDayRowsAsync(string connectionString, int top = 25)
        {
            const string sql = @"SELECT TOP (@top)
    NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '') AS TimeOfDay
FROM dbo.STAGE
WHERE NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '') IS NOT NULL
ORDER BY NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '');";

            var rows = new List<string>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@top", top);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
            }

            return rows;
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

        // Normalize a key for robust matching: trim, upper-case, and remove non-alphanumeric chars.
        private static string NormalizeKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var t = value.Trim().ToUpperInvariant();
            var sb = new StringBuilder(t.Length);
            foreach (var c in t)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

            /// <summary>
            /// Fetches every column from the single DETAL1 row that matches the given shipment key.
            /// Surfaced as "Detal1_AllColumns" in the details pane so the caller can discover which
            /// column names hold TimeOfDay, LoadDate, LoadDoor, LoadPicker, and Stacks.
            /// </summary>
            public static async Task<Dictionary<string, string?>> GetRawDetal1RowAsync(
                string connectionString,
                string shipmentKey)
            {
                const string sql = @"
    SELECT TOP (1) *
    FROM dbo.DETAL1
    WHERE LTRIM(RTRIM([KEY])) = LTRIM(RTRIM(@key))";

                var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@key", shipmentKey.Trim());

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    for (var col = 0; col < reader.FieldCount; col++)
                    {
                        result[reader.GetName(col)] = reader.IsDBNull(col)
                            ? null
                            : Convert.ToString(reader.GetValue(col));
                    }
                }

                return result;
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
            // Match priority for STAGE-derived fields:
            // 1) sExact     – STAGE.[KEY] exactly equals DETAL1.[KEY]
            // 2) sOrderNum  – STAGE.[KEY] exactly equals HEDER1.ORDERNUM
            //                 (most common: STAGE stores the 6-char order number;
            //                  DETAL1 stores order+line, e.g. 123456001)
            // 3) sFallback  – STAGE.[KEY] starts with the first 6 chars of DETAL1.[KEY]
            // 4) sDateFallback – STAGE.LOADDATE date matches HEDER1.SHIPDATE (parsed)
            // 5) HEDER1 raw values used as last resort for LoadDate/Picker
            // Debug columns 16-20 are surfaced in RawDataJson only (not model properties).
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
        TRY_CONVERT(date, LEFT(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sOrderNum.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), 10), 101),
        hParsed.ShipDateParsed
    ) AS LoadDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sOrderNum.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), ''),
        NULLIF(LTRIM(RTRIM(h.SHIPDATE)), '')
    ) AS LoadDateRaw,
    NULLIF(LTRIM(RTRIM(COALESCE(sExact.TIMEOFDAY, sOrderNum.TIMEOFDAY, sFallback.TIMEOFDAY, sDateFallback.TIMEOFDAY))), '') AS TimeOfDay,
    NULLIF(LTRIM(RTRIM(COALESCE(sExact.LOADDOOR, sOrderNum.LOADDOOR, sFallback.LOADDOOR, sDateFallback.LOADDOOR))), '') AS LoadDoor,
    COALESCE(
        NULLIF(LTRIM(RTRIM(COALESCE(sExact.LOADPICKER, sOrderNum.LOADPICKER, sFallback.LOADPICKER, sDateFallback.LOADPICKER))), ''),
        NULLIF(LTRIM(RTRIM(h.PICKERIDNUM)), '')
    ) AS LoadPicker,
    COALESCE(
        TRY_CONVERT(decimal(18,2), NULLIF(LTRIM(RTRIM(CONVERT(varchar(50), COALESCE(sExact.STACKS, sOrderNum.STACKS, sFallback.STACKS, sDateFallback.STACKS)))), '')),
        TRY_CONVERT(decimal(18,2), NULLIF(LTRIM(RTRIM(CONVERT(varchar(50), h.STACKS))), ''))
    ) AS Stacks,
    CASE
        WHEN NULLIF(LTRIM(RTRIM(sExact.[KEY])), '') IS NOT NULL THEN 'STAGE (Exact Key)'
        WHEN NULLIF(LTRIM(RTRIM(sOrderNum.[KEY])), '') IS NOT NULL THEN 'STAGE (Order Number)'
        WHEN NULLIF(LTRIM(RTRIM(sFallback.[KEY])), '') IS NOT NULL THEN 'STAGE (Order Prefix)'
        WHEN NULLIF(LTRIM(RTRIM(sDateFallback.[KEY])), '') IS NOT NULL THEN 'STAGE (Date Match)'
        WHEN NULLIF(LTRIM(RTRIM(h.SHIPDATE)), '') IS NOT NULL THEN 'HEDER1'
        ELSE NULL
    END AS LoadDateSource,
    -- Debug columns (indices 16-20): exposed in RawDataJson for diagnostics, not model props
    ISNULL(LTRIM(RTRIM(CONVERT(varchar(50), h.ORDERNUM))), '') AS DebugOrderNum,
    ISNULL(LTRIM(RTRIM(CONVERT(varchar(50), h.SHIPDATE))), '') AS DebugHederShipDate,
    ISNULL(LTRIM(RTRIM(sExact.[KEY])), '') AS DebugKeyExact,
    ISNULL(LTRIM(RTRIM(sOrderNum.[KEY])), '') AS DebugKeyOrder,
    ISNULL(LTRIM(RTRIM(sFallback.[KEY])), '') AS DebugKeyFallback
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
    -- Tier 1: STAGE.[KEY] exactly equals DETAL1.[KEY]
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS
    FROM dbo.STAGE s
    WHERE LTRIM(RTRIM(s.[KEY])) = LTRIM(RTRIM(d.[KEY]))
) sExact
OUTER APPLY (
    -- Tier 2: STAGE.[KEY] equals the header order number from HEDER1
    --         (handles the common case where STAGE uses the 6-char order number
    --          but DETAL1 uses order+line, e.g. DETAL1.KEY='123456001', STAGE.KEY='123456')
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS
    FROM dbo.STAGE s
    WHERE h.ORDERNUM IS NOT NULL
      AND LTRIM(RTRIM(s.[KEY])) = LTRIM(RTRIM(h.ORDERNUM))
    ORDER BY
        CASE WHEN NULLIF(LTRIM(RTRIM(s.LOADDATE)), '') IS NULL THEN 1 ELSE 0 END,
        LTRIM(RTRIM(s.[KEY]))
) sOrderNum
OUTER APPLY (
    -- Tier 3: STAGE.[KEY] contains the order number (h.ORDERNUM) anywhere in it.
    --         Uses CONTAINS LIKE so keys like 'ABC123456DEF' or '123456-01' are found.
    --         Ordered by key length ASC so the shortest (most-specific) match wins.
    --         Excludes the exact-order-number match already captured by sOrderNum.
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS
    FROM dbo.STAGE s
    WHERE h.ORDERNUM IS NOT NULL
      AND LTRIM(RTRIM(s.[KEY])) LIKE '%' + LTRIM(RTRIM(CAST(h.ORDERNUM AS varchar(20)))) + '%'
      AND LTRIM(RTRIM(s.[KEY])) != LTRIM(RTRIM(CAST(h.ORDERNUM AS varchar(20))))
    ORDER BY
        CASE WHEN NULLIF(LTRIM(RTRIM(s.LOADDATE)), '') IS NULL THEN 1 ELSE 0 END,
        LEN(LTRIM(RTRIM(s.[KEY]))),
        LTRIM(RTRIM(s.[KEY]))
) sFallback
OUTER APPLY (
    -- Tier 4: STAGE.LOADDATE date matches the ship date parsed from HEDER1
    SELECT TOP (1)
        s.[KEY],
        s.LOADDATE,
        s.TIMEOFDAY,
        s.LOADDOOR,
        s.LOADPICKER,
        s.STACKS
    FROM dbo.STAGE s
    WHERE hParsed.ShipDateParsed IS NOT NULL
      AND TRY_CONVERT(date, LEFT(LTRIM(RTRIM(s.LOADDATE)), 10), 101) = hParsed.ShipDateParsed
    ORDER BY
        CASE WHEN NULLIF(LTRIM(RTRIM(s.LOADDATE)), '') IS NULL THEN 1 ELSE 0 END,
        LTRIM(RTRIM(s.[KEY]))
) sDateFallback
WHERE d.ITEMNUM = @itemNumber
ORDER BY
    CASE WHEN COALESCE(
        TRY_CONVERT(date, LEFT(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sOrderNum.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), 10), 101),
        hParsed.ShipDateParsed
    ) IS NULL THEN 1 ELSE 0 END,
    COALESCE(
        TRY_CONVERT(date, LEFT(LTRIM(RTRIM(COALESCE(sExact.LOADDATE, sOrderNum.LOADDATE, sFallback.LOADDATE, sDateFallback.LOADDATE))), 10), 101),
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
                var normalized = NormalizeKey(shipmentKey);
                var loadDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
                var quantityShipped = reader.GetDecimal(3);
                var pricePerUnit = reader.GetDecimal(5);
                // Build raw dictionary using SQL alias names so the JSON is self-documenting.
                var raw = new Dictionary<string, object?>();
                for (var col = 0; col < reader.FieldCount; col++)
                {
                    raw[reader.GetName(col)] = reader.IsDBNull(col)
                        ? null
                        : reader.GetValue(col)?.ToString();
                }
                // Serialize and enforce a max size to avoid overly large payloads.
                var rawJson = JsonSerializer.Serialize(raw, new JsonSerializerOptions { WriteIndented = true });
                const int MaxRawJsonLength = 16000;
                if (rawJson.Length > MaxRawJsonLength)
                {
                    rawJson = rawJson.Substring(0, MaxRawJsonLength) + "\n...truncated...";
                }

                var shipment = new ShipmentListItem
                {
                    ShipmentKey = shipmentKey,
                    NormalizedKey = normalized,
                    RawDataJson = rawJson,
                    LoadDate = loadDate,
                    LoadDateRaw = reader.IsDBNull(10) ? null : reader.GetString(10),
                    LoadDateSource = reader.IsDBNull(15) ? null : reader.GetString(15),
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
                        NormalizedKey = normalized,
                        RawDataJson = rawJson,
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
                        LoadDateSource = reader.IsDBNull(15) ? null : reader.GetString(15),
                        TimeOfDay = reader.IsDBNull(11) ? null : reader.GetString(11),
                        LoadDoor = reader.IsDBNull(12) ? null : reader.GetString(12),
                        LoadPicker = reader.IsDBNull(13) ? null : reader.GetString(13),
                        Stacks = reader.IsDBNull(14) ? (decimal?)null : reader.GetDecimal(14)
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
