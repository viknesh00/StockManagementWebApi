using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.NonStockCII;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class SmInboundStockNonCiisController : BaseApiController
	{
		private readonly INonCiiStockService _nonCiiStockService;

		public SmInboundStockNonCiisController(INonCiiStockService nonCiiStockService)
		{
			_nonCiiStockService = nonCiiStockService;
		}

		// ------------------------------------------------------------------ Listings

		// GET: api/SmInboundStockNonCiis/GetSmInboundNonStockCiis/{UserName}
		[HttpGet("GetSmInboundNonStockCiis/{UserName}")]
		public async Task<IActionResult> GetSmInboundNonStockCiis(string UserName, CancellationToken cancellationToken)
		{
			var stock = await _nonCiiStockService.GetStockListAsync(UserName, cancellationToken);

			return Success(stock, "Non-CII stock list retrieved successfully.");
		}

		[HttpGet("GetInwardNonStockCiis/{MaterialNumber}/{name}")]
		public async Task<IActionResult> GetInwardNonStockCiis(string MaterialNumber, string name, CancellationToken cancellationToken)
		{
			var inward = await _nonCiiStockService.GetInwardListAsync(MaterialNumber, name, cancellationToken);

			return Success(inward, "Non-CII inward list retrieved successfully.");
		}

		// ------------------------------------------------------------------ Bulk operations

		[HttpPost("bulk-update")]
		public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequest request, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.BulkUpdateSerialsAsync(request, cancellationToken);

			return Updated(message: "Updated successfully");
		}

		[HttpPost("BulkImportNonCII")]
		public async Task<IActionResult> BulkImportNonCII([FromForm] AddNonCIIStockInward data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.BulkImportAsync(data, cancellationToken);

			return Success(message: "Bulk upload completed successfully.");
		}

		// ------------------------------------------------------------------ Material master

		[HttpPost("NonStockCIIMaterial")]
		public async Task<IActionResult> AddMaterialNumber([FromBody] AddMaterial data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddMaterialAsync(data, cancellationToken);

			return Success(message: "Material number added successfully.");
		}

		// ------------------------------------------------------------------ Inbound

		[HttpPost("AddNonStockInbounddata")]
		public async Task<IActionResult> AddNonStockInbounddata([FromBody] AddNonStockInward data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddInboundAsync(data, cancellationToken);

			return Success(message: "Inbound data added successfully.");
		}

		[HttpPost("DeleteNonStockInbounddata/{MaterialNumber}/{DeliveryNumber}/{InboundStockNonCIIKey}/{name}")]
		public async Task<IActionResult> DeleteNonStockInbounddata(string MaterialNumber, string DeliveryNumber, string InboundStockNonCIIKey, string name, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteInboundAsync(MaterialNumber, DeliveryNumber, InboundStockNonCIIKey, name, cancellationToken);

			return Deleted(message: "Inbound data deleted successfully.");
		}

		[HttpPost("UpdateNonStockInbounddata")]
		public async Task<IActionResult> UpdateNonStockInbounddata([FromBody] UpdateNonStockInward data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateInboundAsync(data, cancellationToken);

			return Updated(message: "Inbound data updated successfully.");
		}

		// ------------------------------------------------------------------ Outbound

		[HttpPost("AddNonStockOutbounddata")]
		public async Task<IActionResult> AddNonStockOutbounddata([FromBody] AddOutBoundNonStockCII data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddOutboundAsync(data, cancellationToken);

			return Success(message: "Outbound data added successfully.");
		}

		[HttpPost("BulkAddNonStockOutbound")]
		public async Task<IActionResult> BulkAddNonStockOutbound([FromBody] List<BulkAddOutboundDataNonStockCii> dataList, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.BulkAddOutboundAsync(dataList, cancellationToken);

			return Success(message: "Bulk upload completed successfully.");
		}

		[HttpGet("DeliveredDataList/{MaterialNumber}")]
		public async Task<IActionResult> GetDeliveredDataList(string MaterialNumber, CancellationToken cancellationToken)
		{
			var delivered = await _nonCiiStockService.GetDeliveredListAsync(MaterialNumber, cancellationToken);

			return Success(delivered, "Delivered data retrieved successfully.");
		}

		[HttpPost("DeleteNonStockDeliverdata/{MaterialNumber}/{DeliveryNumber}/{OutboundStockNonCIIKey}/{UserName}")]
		public async Task<IActionResult> DeleteNonStockDeliverdata(string MaterialNumber, string DeliveryNumber, string OutboundStockNonCIIKey, string UserName, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteDeliveredAsync(MaterialNumber, DeliveryNumber, OutboundStockNonCIIKey, UserName, cancellationToken);

			return Deleted(message: "Non-stock delivery data deleted and inbound stock updated successfully.");
		}

		[HttpPost("UpdateNonStockDeliverdata")]
		public async Task<IActionResult> UpdateNonStockDeliverdata([FromBody] UpdateNonStockDeliverData data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateDeliveredAsync(data, cancellationToken);

			return Updated(message: "Inbound and outbound stock updated successfully.");
		}

		// ------------------------------------------------------------------ Returns

		[HttpPost("AddNonStockReturndata")]
		public async Task<IActionResult> AddNonStockReturndata([FromBody] AddNonStockReturnData data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddReturnAsync(data, cancellationToken);

			return Success(message: "Return data added successfully.");
		}

		[HttpPost("UpdateNonStockReturndata")]
		public async Task<IActionResult> UpdateNonStockReturndata([FromBody] UpdateNonStockRetundata data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateReturnAsync(data, cancellationToken);

			return Updated(message: "Return data updated successfully.");
		}

		[HttpPost("UpdateNonStockReturnType/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> UpdateNonStockReturnType(string MaterialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateReturnTypeAsync(MaterialNumber, OrderNumber, cancellationToken);

			return Updated(message: "Return type updated successfully.");
		}

		[HttpPost("DeleteNonStockReturnData/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockReturnData(string MaterialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteReturnAsync(MaterialNumber, OrderNumber, cancellationToken);

			return Deleted(message: "Return data deleted successfully.");
		}

		[HttpPost("GetNonStockReturnData/{MaterialNumber}")]
		public async Task<IActionResult> GetNonStockReturnData(string MaterialNumber, CancellationToken cancellationToken)
		{
			var returns = await _nonCiiStockService.GetReturnListAsync(MaterialNumber, cancellationToken);

			return Success(returns, "Return data retrieved successfully.");
		}

		// ------------------------------------------------------------------ Used stock

		[HttpPost("GetNonStockUsedData/{MaterialNumber}")]
		public async Task<IActionResult> GetNonStockUsedData(string MaterialNumber, CancellationToken cancellationToken)
		{
			var usedStock = await _nonCiiStockService.GetUsedStockListAsync(MaterialNumber, cancellationToken);

			return Success(usedStock, "Used stock retrieved successfully.");
		}

		[HttpPost("AddNonStockUsedData")]
		public async Task<IActionResult> AddNonStockUsedData([FromBody] AddUsedStock data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddUsedStockAsync(data, cancellationToken);

			return Success(message: "Used stock added successfully.");
		}

		[HttpPost("UpdateNonStockUsedData")]
		public async Task<IActionResult> UpdateNonStockUsedData([FromBody] UpdateUsedStock data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateUsedStockAsync(data, cancellationToken);

			return Updated(message: "Used stock updated successfully.");
		}

		[HttpPost("DeleteNonStockUsedData/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockUsedData(string MaterialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteUsedStockAsync(MaterialNumber, OrderNumber, cancellationToken);

			return Deleted(message: "Used stock deleted successfully.");
		}

		// ------------------------------------------------------------------ Dashboards

		[HttpPost("DashBoard/{UserName}")]
		public async Task<IActionResult> DashBoard(string UserName, CancellationToken cancellationToken)
		{
			var dashboard = await _nonCiiStockService.GetDashboardAsync(UserName, cancellationToken);

			return Success(dashboard, "Dashboard retrieved successfully.");
		}

		[HttpPost("AnalyticsCII/{MaterialNumber}")]
		public async Task<IActionResult> AnalyticsCII(string MaterialNumber, CancellationToken cancellationToken)
		{
			var analytics = await _nonCiiStockService.GetCiiAnalyticsAsync(MaterialNumber, cancellationToken);

			return Success(analytics, "CII analytics retrieved successfully.");
		}

		[HttpPost("AnalyticsNonCII/{MaterialNumber}")]
		public async Task<IActionResult> AnalyticsNonCII(string MaterialNumber, CancellationToken cancellationToken)
		{
			var analytics = await _nonCiiStockService.GetNonCiiAnalyticsAsync(MaterialNumber, cancellationToken);

			return Success(analytics, "Non-CII analytics retrieved successfully.");
		}

		[HttpPost("DashBoardCharts/{Username}")]
		public async Task<IActionResult> DashBoardCharts(string Username, CancellationToken cancellationToken)
		{
			var charts = await _nonCiiStockService.GetDashboardChartsAsync(Username, cancellationToken);

			return Success(charts, "Dashboard charts retrieved successfully.");
		}

		// ------------------------------------------------------------------ Entity CRUD

		// GET: api/SmInboundStockNonCiis/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetSmInboundStockNonCii(string id, CancellationToken cancellationToken)
		{
			var stock = await _nonCiiStockService.GetByIdAsync(id, cancellationToken);

			return Success(stock, "Inbound non-CII stock retrieved successfully.");
		}

		// PUT: api/SmInboundStockNonCiis/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutSmInboundStockNonCii(string id, SmInboundStockNonCii smInboundStockNonCii, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateAsync(id, smInboundStockNonCii, cancellationToken);

			return Updated(message: "Inbound non-CII stock updated successfully.");
		}

		// POST: api/SmInboundStockNonCiis
		[HttpPost]
		public async Task<IActionResult> PostSmInboundStockNonCii(SmInboundStockNonCii smInboundStockNonCii, CancellationToken cancellationToken)
		{
			var created = await _nonCiiStockService.CreateAsync(smInboundStockNonCii, cancellationToken);

			return Created(created, "Inbound non-CII stock created successfully.", Url.Action(nameof(GetSmInboundStockNonCii), new { id = created.DeliveryNumber }));
		}

		// DELETE: api/SmInboundStockNonCiis/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSmInboundStockNonCii(string id, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteAsync(id, cancellationToken);

			return Deleted(message: "Inbound non-CII stock deleted successfully.");
		}
	}
}
