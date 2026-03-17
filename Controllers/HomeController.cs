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