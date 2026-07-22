using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class SmOutboundStockCiisController : BaseApiController
	{
		private readonly IOutboundStockCiiService _outboundStockService;

		public SmOutboundStockCiisController(IOutboundStockCiiService outboundStockService)
		{
			_outboundStockService = outboundStockService;
		}

		// GET: api/SmOutboundStockCiis
		[HttpGet]
		public async Task<IActionResult> GetSmOutboundStockCiis(CancellationToken cancellationToken)
		{
			var stock = await _outboundStockService.GetAllAsync(cancellationToken);

			return Success(stock, "Outbound CII stock retrieved successfully.");
		}

		[HttpPost("CollectionPointUpdate")]
		public async Task<IActionResult> CollectionPointUpdate([FromBody] CollectionPointDetail data, CancellationToken cancellationToken)
		{
			await _outboundStockService.UpdateCollectionPointAsync(data, cancellationToken);

			return Updated(message: "Collection point updated successfully.");
		}

		[HttpPost("{MaterialNumber}/{SerialNumber}/{OrderNumber}")]
		public async Task<IActionResult> outboundstockList(string MaterialNumber, string SerialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			var result = await _outboundStockService.GetStockLookupAsync(MaterialNumber, SerialNumber, OrderNumber, cancellationToken);

			return Success(result, "Stock details retrieved successfully.");
		}

		// ------------------------------------------------------------------ Outward

		[HttpPost("AddOutboundData")]
		public async Task<IActionResult> AddOutboundData([FromBody] AddDeliveryData data, CancellationToken cancellationToken)
		{
			await _outboundStockService.AddOutboundDataAsync(data, cancellationToken);

			return Success(message: "Outward data added successfully.");
		}

		[HttpPost("BulkOutwardData")]
		public async Task<IActionResult> BulkOutwardData([FromBody] BulkAddDeliveryData data, CancellationToken cancellationToken)
		{
			await _outboundStockService.AddBulkOutboundDataAsync(data, cancellationToken);

			return Success(message: "Outward data added successfully.");
		}

		[HttpPost("DeleteOutboundData/{MaterialNumber}/{SerialNumber}/{OutBoundStockCIIKey}")]
		public async Task<IActionResult> DeleteOutboundData(string MaterialNumber, string SerialNumber, int OutBoundStockCIIKey, CancellationToken cancellationToken)
		{
			await _outboundStockService.DeleteOutboundDataAsync(MaterialNumber, SerialNumber, OutBoundStockCIIKey, cancellationToken);

			return Deleted(message: "Outbound data deleted successfully.");
		}

		[HttpPost("UpdatedeliveryData")]
		public async Task<IActionResult> UpdatedeliveryData([FromBody] UpdatedeliveryDataList data, CancellationToken cancellationToken)
		{
			await _outboundStockService.UpdateDeliveryDataAsync(data, cancellationToken);

			return Updated(message: "Delivery data updated successfully.");
		}

		// ------------------------------------------------------------------ Returns

		[HttpPost("AddReturnData")]
		public async Task<IActionResult> AddReturnData([FromBody] AddReturnDataList data, CancellationToken cancellationToken)
		{
			await _outboundStockService.AddReturnDataAsync(data, cancellationToken);

			return Success(message: "Return data added successfully.");
		}

		[HttpPost("UpdateReturnData")]
		public async Task<IActionResult> UpdateReturnData([FromBody] UpdateReturnDataList data, CancellationToken cancellationToken)
		{
			await _outboundStockService.UpdateReturnDataAsync(data, cancellationToken);

			return Updated(message: "Return data updated successfully.");
		}

		[HttpPost("DeleteReturnData/{MaterialNumber}/{SerialNumber}/{ReturnStockCIIKey}")]
		public async Task<IActionResult> DeleteReturnData(string MaterialNumber, string SerialNumber, int ReturnStockCIIKey, CancellationToken cancellationToken)
		{
			await _outboundStockService.DeleteReturnDataAsync(MaterialNumber, SerialNumber, ReturnStockCIIKey, cancellationToken);

			return Deleted(message: "Return data deleted successfully.");
		}

		// ------------------------------------------------------------------ Staging

		[HttpPost("AddStaging")]
		public async Task<IActionResult> AddStaging([FromBody] StagingModel data, CancellationToken cancellationToken)
		{
			await _outboundStockService.AddStagingAsync(data, cancellationToken);

			return Success(message: "Staging record added successfully.");
		}

		[HttpPut("UpdateStaging")]
		public async Task<IActionResult> UpdateStaging([FromBody] StagingModel data, CancellationToken cancellationToken)
		{
			await _outboundStockService.UpdateStagingAsync(data, cancellationToken);

			return Updated(message: "Staging record updated successfully.");
		}

		[HttpDelete("DeleteStaging/{id}")]
		public async Task<IActionResult> DeleteStaging(int id, CancellationToken cancellationToken)
		{
			await _outboundStockService.DeleteStagingAsync(id, cancellationToken);

			return Deleted(message: "Staging record deleted successfully.");
		}

		// ------------------------------------------------------------------ Entity CRUD

		// GET: api/SmOutboundStockCiis/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetSmOutboundStockCii(string id, CancellationToken cancellationToken)
		{
			var stock = await _outboundStockService.GetByIdAsync(id, cancellationToken);

			return Success(stock, "Outbound CII stock retrieved successfully.");
		}

		// PUT: api/SmOutboundStockCiis/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutSmOutboundStockCii(string id, SmOutboundStockCii smOutboundStockCii, CancellationToken cancellationToken)
		{
			await _outboundStockService.UpdateAsync(id, smOutboundStockCii, cancellationToken);

			return Updated(message: "Outbound CII stock updated successfully.");
		}

		// POST: api/SmOutboundStockCiis
		[HttpPost]
		public async Task<IActionResult> PostSmOutboundStockCii(SmOutboundStockCii smOutboundStockCii, CancellationToken cancellationToken)
		{
			var created = await _outboundStockService.CreateAsync(smOutboundStockCii, cancellationToken);

			return Created(created, "Outbound CII stock created successfully.", Url.Action(nameof(GetSmOutboundStockCii), new { id = created.DeliveryNumber }));
		}

		// DELETE: api/SmOutboundStockCiis/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSmOutboundStockCii(string id, CancellationToken cancellationToken)
		{
			await _outboundStockService.DeleteAsync(id, cancellationToken);

			return Deleted(message: "Outbound CII stock deleted successfully.");
		}
	}
}
