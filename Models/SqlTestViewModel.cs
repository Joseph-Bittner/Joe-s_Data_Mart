namespace DeveloperJosephBittner.DataMart.Models
{
    public class SqlTestViewModel
    {
        public string Query { get; set; } = "SELECT TOP (100) NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '') AS TimeOfDay FROM dbo.STAGE WHERE NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '') IS NOT NULL ORDER BY NULLIF(LTRIM(RTRIM(TIMEOFDAY)), '');";
        public List<string> TimeOfDayResults { get; set; } = new();
        public string? Error { get; set; }
    }
}