using System;
using DeveloperJosephBittner.DataMart;

namespace DeveloperJosephBittner.DataMart.Models
{
    /// <summary>
    /// View model for the Ship Count page form, results, and validation messages.
    /// </summary>
    public sealed class ShipCountViewModel
    {
        /// <summary>
        /// Item number entered by the user.
        /// </summary>
        public string ItemNumber { get; set; } = string.Empty;

        /// <summary>
        /// Optional shipment key used when a specific row is selected.
        /// </summary>
        public string? SelectedShipmentKey { get; set; }

        /// <summary>
        /// Query result payload returned from the data client.
        /// </summary>
        public DataMartClient.ShipCountResult? Result { get; set; }

        /// <summary>
        /// User-facing error message shown when validation or query execution fails.
        /// </summary>
        public string? Error { get; set; }
    }
}