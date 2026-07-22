namespace StockManagementWebApi.Models.Responses
{
	/// <summary>
	/// Everything known about one material/serial combination across the inbound, outbound,
	/// return and staging tables. Property names match the anonymous object this endpoint
	/// returned previously, so the serialized shape is unchanged.
	/// </summary>
	public class OutboundStockLookupResult
	{
		public IReadOnlyList<InboundCIIList> CIIData { get; set; } = Array.Empty<InboundCIIList>();

		public IReadOnlyList<ReturnStockData> InboundData { get; set; } = Array.Empty<ReturnStockData>();

		public IReadOnlyList<OutboundDataList> DeliveryData { get; set; } = Array.Empty<OutboundDataList>();

		public IReadOnlyList<StagingStockData> StagingData { get; set; } = Array.Empty<StagingStockData>();
	}

	/// <summary>Dashboard tile counts.</summary>
	public class DashboardSummary
	{
		public IReadOnlyList<DashboardList> CIICounts { get; set; } = Array.Empty<DashboardList>();

		public IReadOnlyList<DashboardList> NonCIICounts { get; set; } = Array.Empty<DashboardList>();

		public IReadOnlyList<DashboardDeliveryCount> DeliveryReturnCounts { get; set; } = Array.Empty<DashboardDeliveryCount>();
	}

	/// <summary>Dashboard chart series.</summary>
	public class DashboardChartSummary
	{
		public IReadOnlyList<Dashboardchart> CIIInwardCounts { get; set; } = Array.Empty<Dashboardchart>();

		public IReadOnlyList<Dashboardchart> NonCIIInwardCounts { get; set; } = Array.Empty<Dashboardchart>();

		public IReadOnlyList<Dashboardchart> CIIDeliveryCounts { get; set; } = Array.Empty<Dashboardchart>();

		public IReadOnlyList<Dashboardchart> NonCIIDeliveryCounts { get; set; } = Array.Empty<Dashboardchart>();
	}
}
