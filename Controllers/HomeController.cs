using DeveloperJosephBittner.DataMart;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperJosephBittner.DataMart.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
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
        public IActionResult Error()
        {
            return View();
        }
    }
}