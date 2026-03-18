using System.Text;
using System.Text.Encodings.Web;
using DeveloperJosephBittner.DataMart;
using DeveloperJosephBittner.DataMart.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperJosephBittner.DataMart.Controllers
{
    /// <summary>
    /// Serves the main pages for Data Mart and handles Ship Count search requests.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Displays the home page.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        /// <summary>
        /// Displays the Ship Count page with an empty form.
        /// </summary>
        public IActionResult ShipCount()
        {
            return View(new ShipCountViewModel());
        }

        [HttpPost]
        /// <summary>
        /// Executes the Ship Count search and returns shipment results for a single item number.
        /// </summary>
        public async Task<IActionResult> ShipCount(ShipCountViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ItemNumber))
            {
                model.Error = "Item number is required.";
                return View(model);
            }

            // Allow a deployment-specific connection string, with a safe default for local execution.
            var connectionString = Environment.GetEnvironmentVariable("DATA_MART_CONNECTION_STRING")
                ?? DataMartClient.BuildConnectionString();

            try
            {
                var result = await DataMartClient.GetShipmentsByItemAsync(
                    connectionString,
                    model.ItemNumber.Trim(),
                    model.SelectedShipmentKey);

                model.Result = result;
            }
            catch (Exception ex)
            {
                model.Error = ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        /// <summary>
        /// Lightweight connectivity test used by the home page.
        /// </summary>
        public async Task<IActionResult> TestConnection()
        {
            var connectionString = Environment.GetEnvironmentVariable("DATA_MART_CONNECTION_STRING")
                ?? DataMartClient.BuildConnectionString();

            try
            {
                var tables = await DataMartClient.GetTopTablesAsync(connectionString, 10);
                ViewBag.Status = "Connected successfully";
                ViewBag.Tables = tables;
            }
            catch (Exception ex)
            {
                ViewBag.Status = "Connection failed";
                ViewBag.Error = ex.Message;
            }

            return View("Index");
        }

        [HttpGet]
        /// <summary>
        /// Displays a simple SQL test page for executing a TimeOfDay query.
        /// </summary>
        public IActionResult SqlTest()
        {
            return View(new SqlTestViewModel());
        }

        [HttpPost]
        /// <summary>
        /// Runs a lightweight SQL query and displays the TimeOfDay result.
        /// </summary>
        public async Task<IActionResult> SqlTest(SqlTestViewModel model)
        {
            model.Query = "SELECT TOP (25) NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '') AS TimeOfDay FROM dbo.STAGE WHERE NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '') IS NOT NULL ORDER BY NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '');";

            var connectionString = Environment.GetEnvironmentVariable("DATA_MART_CONNECTION_STRING")
                ?? DataMartClient.BuildConnectionString();

            try
            {
                model.TimeOfDayResults = await DataMartClient.GetSampleTimeOfDayRowsAsync(connectionString, 25);
                model.Error = null;
            }
            catch (Exception ex)
            {
                model.TimeOfDayResults.Clear();
                model.Error = ex.Message;
            }

            return View(model);
        }

        [HttpGet]
        /// <summary>
        /// Returns a single shipment detail as JSON for client-side display.
        /// Runs the main query and the raw DETAL1 row lookup concurrently so the
        /// caller receives both the enriched model fields AND every raw column from
        /// the DETAL1 row (Detal1_AllColumns) in one round-trip.
        /// </summary>
        public async Task<IActionResult> GetShipmentDetails(string itemNumber, string shipmentKey)
        {
            if (string.IsNullOrWhiteSpace(itemNumber) || string.IsNullOrWhiteSpace(shipmentKey))
            {
                return BadRequest("itemNumber and shipmentKey are required");
            }

            var connectionString = Environment.GetEnvironmentVariable("DATA_MART_CONNECTION_STRING")
                ?? DataMartClient.BuildConnectionString();

            try
            {
                var detailTask = DataMartClient.GetShipmentsByItemAsync(connectionString, itemNumber.Trim(), shipmentKey.Trim());
                var rawRowTask = DataMartClient.GetRawDetal1RowAsync(connectionString, shipmentKey.Trim());
                await Task.WhenAll(detailTask, rawRowTask);

                var result = detailTask.Result;
                if (result?.SelectedShipment == null)
                {
                    return NotFound();
                }

                var s = result.SelectedShipment;
                return Json(new
                {
                    s.ShipmentKey,
                    s.ItemNumber,
                    s.Description,
                    s.LoadDate,
                    s.LoadDateRaw,
                    s.LoadDateSource,
                    s.TimeOfDay,
                    s.LoadDoor,
                    s.LoadPicker,
                    s.Stacks,
                    s.QuantityShipped,
                    s.QuantityOrdered,
                    s.PricePerUnit,
                    s.TotalCostShipped,
                    s.TotalShipNetWeight,
                    s.ProductType,
                    s.ItemCategoryGroup,
                    s.NormalizedKey,
                    s.RawDataJson,
                    Detal1_AllColumns = rawRowTask.Result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        /// <summary>
        /// Exports the enriched ShipCount rows as CSV or JSON.
        /// </summary>
        public async Task<IActionResult> ExportShipCount(string itemNumber, string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(itemNumber))
            {
                return BadRequest("itemNumber is required");
            }

            var connectionString = Environment.GetEnvironmentVariable("DATA_MART_CONNECTION_STRING")
                ?? DataMartClient.BuildConnectionString();

            var result = await DataMartClient.GetShipmentsByItemAsync(connectionString, itemNumber.Trim(), null);

            if (format?.ToLowerInvariant() == "json")
            {
                return Json(result.Shipments);
            }

            // Build CSV
            var sb = new StringBuilder();
            sb.AppendLine("ShipmentKey,ItemNumber,Description,LoadDate,LoadDateSource,TimeOfDay,QtyShipped,QtyOrdered,PricePerUnit,TotalCost,TotalShipNetWeight,Stacks");
            foreach (var s in result.Shipments)
            {
                var line = string.Join(",",
                    EscapeCsv(s.ShipmentKey),
                    EscapeCsv(s.Description ?? string.Empty),
                    EscapeCsv(s.LoadDate?.ToString("yyyy-MM-dd") ?? string.Empty),
                    EscapeCsv(s.LoadDateSource ?? string.Empty),
                    EscapeCsv(s.TimeOfDay ?? string.Empty),
                    s.QuantityShipped.ToString("F2"),
                    s.QuantityOrdered.ToString("F2"),
                    s.PricePerUnit.ToString("F2"),
                    s.TotalCostShipped.ToString("F2"),
                    s.TotalShipNetWeight.ToString("F2"),
                    (s.Stacks.HasValue ? s.Stacks.Value.ToString("F2") : string.Empty)
                );
                sb.AppendLine(line);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"ShipCount_{itemNumber.Trim()}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet]
        /// <summary>
        /// Returns raw row JSON blobs for all shipment rows of a given item.
        /// This is a temporary diagnostics endpoint while load-related fields are under repair.
        /// </summary>
        public async Task<IActionResult> RawRows(string itemNumber)
        {
            if (string.IsNullOrWhiteSpace(itemNumber))
            {
                return BadRequest("itemNumber is required");
            }

            var connectionString = Environment.GetEnvironmentVariable("DATA_MART_CONNECTION_STRING")
                ?? DataMartClient.BuildConnectionString();

            var result = await DataMartClient.GetShipmentsByItemAsync(connectionString, itemNumber.Trim(), null);

            var payload = new
            {
                ItemNumber = result.ItemNumber,
                ShipmentCount = result.Shipments.Count,
                Rows = result.Shipments.Select(s => new
                {
                    s.ShipmentKey,
                    s.NormalizedKey,
                    s.RawDataJson
                })
            };

            return Json(payload);
        }

        private static string EscapeCsv(string value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return '"' + value.Replace("\"", "\"\"") + '"';
            }
            return value;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        /// <summary>
        /// Standard error endpoint used by ASP.NET Core when unhandled exceptions occur.
        /// </summary>
        public IActionResult Error()
        {
            return View();
        }
    }
}