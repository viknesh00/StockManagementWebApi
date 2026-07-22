using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.NonStockCII;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class SmInboundStockCiisController : BaseApiController
	{
		private readonly IInboundStockCiiService _inboundStockService;

		public SmInboundStockCiisController(IInboundStockCiiService inboundStockService)
		{
			_inboundStockService = inboundStockService;
		}

		// ------------------------------------------------------------------ Listings

		// GET: api/SmInboundStockCiis/GetSmInboundStockCiis/{UserName}
		[HttpGet("GetSmInboundStockCiis/{UserName}")]
		public async Task<IActionResult> GetSmInboundStockCiis(string UserName, CancellationToken cancellationToken)
		{
			var stock = await _inboundStockService.GetStockListAsync(UserName, cancellationToken);

			return Success(stock, "CII stock list retrieved successfully.");
		}

		[HttpGet("GetReportStockCiis/{UserName}")]
		public async Task<IActionResult> GetReportStockCiis(string UserName, CancellationToken cancellationToken)
		{
			var report = await _inboundStockService.GetReportAsync(UserName, cancellationToken);

			return Success(report, "CII stock report retrieved successfully.");
		}

		[HttpGet("GetLogmanagementRecord")]
		public async Task<IActionResult> GetLogmanagementRecord(CancellationToken cancellationToken)
		{
			var logRecords = await _inboundStockService.GetLogRecordsAsync(cancellationToken);

			return Success(logRecords, "Log records retrieved successfully.");
		}

		[HttpGet("GetOverallCIIStock/{UserName}")]
		public async Task<IActionResult> GetOverallCIIStock(string UserName, CancellationToken cancellationToken)
		{
			var stock = await _inboundStockService.GetOverallStockAsync(UserName, cancellationToken);

			return Success(stock, "Overall CII stock retrieved successfully.");
		}

		[HttpPost("SearchSerialNumber/{username}/{SerialNumber}")]
		public async Task<IActionResult> GetSmInboundStockCii(string username, string SerialNumber, CancellationToken cancellationToken)
		{
			var results = await _inboundStockService.SearchBySerialNumberAsync(username, SerialNumber, cancellationToken);

			return Success(results, "Serial number search completed successfully.");
		}

		// GET: api/SmInboundStockCiis/{MaterialNumber}/{SerialNumber}/{name}
		[HttpGet("{MaterialNumber}/{SerialNumber}/{name}")]
		public async Task<IActionResult> GetSmInboundStockCiii(string MaterialNumber, string? SerialNumber, string name, CancellationToken cancellationToken)
		{
			var results = await _inboundStockService.GetByMaterialAndSerialAsync(MaterialNumber, SerialNumber, name, cancellationToken);

			return Success(results, "CII stock retrieved successfully.");
		}

		// ------------------------------------------------------------------ Imports

		[HttpPost("compare")]
		public async Task<IActionResult> CompareMaterials([FromForm] ExcelCompareRequest data, CancellationToken cancellationToken)
		{
			var results = await _inboundStockService.CompareMaterialsAsync(data, cancellationToken);

			return Success(results, "Material comparison completed successfully.");
		}

		[HttpPost("AddBulkMaterialStock")]
		public async Task<IActionResult> Importstockdate1(AddStockInward data, CancellationToken cancellationToken)
		{
			await _inboundStockService.ImportBulkMaterialStockAsync(data, cancellationToken);

			return Success(message: "Data imported successfully.");
		}

		[HttpPost("import")]
		public async Task<IActionResult> ImportStockData(AddStockInward data, CancellationToken cancellationToken)
		{
			await _inboundStockService.ImportStockDataAsync(data, cancellationToken);

			return Success(message: "Data imported successfully.");
		}

		[HttpPost("ImportSingleStockData")]
		public async Task<IActionResult> ImportSingleStockDataF([FromBody] AddSingleStockInward data, CancellationToken cancellationToken)
		{
			await _inboundStockService.ImportSingleStockAsync(data, cancellationToken);

			return Success(message: "Data imported successfully.");
		}

		[HttpPost("UpdateInbounddata")]
		public async Task<IActionResult> UpdateInbounddata([FromBody] List<UpdateInboundData> data, CancellationToken cancellationToken)
		{
			var updated = await _inboundStockService.UpdateInboundDataAsync(data, cancellationToken);

			return updated == null
				? Success(message: "Bulk Data Uploaded Successfully")
				: Updated(updated, "Inbound data updated successfully.");
		}

		// ------------------------------------------------------------------ Material master

		[HttpPost("Material")]
		public async Task<IActionResult> AddMaterialNumber([FromBody] AddMaterial data, CancellationToken cancellationToken)
		{
			await _inboundStockService.AddMaterialAsync(data, cancellationToken);

			return Success(message: "Material number added successfully.");
		}

		[HttpPost("update")]
		public async Task<IActionResult> UpdateMaterialNumber([FromBody] UpdateAddMaterial data, CancellationToken cancellationToken)
		{
			await _inboundStockService.UpdateMaterialAsync(data, cancellationToken);

			return Updated(message: "Material number updated successfully.");
		}

		[HttpPost("{MaterialNumber}/{IsActive}/{userName}")]
		public async Task<IActionResult> deleteMaterialNumber(string MaterialNumber, bool IsActive, string userName, CancellationToken cancellationToken)
		{
			await _inboundStockService.DeleteMaterialAsync(MaterialNumber, IsActive, userName, cancellationToken);

			return Deleted(message: "Material number status updated successfully.");
		}

		// ------------------------------------------------------------------ Serial numbers

		[HttpPost("serial/{MaterialNumber}/{SerialNumber}")]
		public async Task<IActionResult> deleteSerialNumber(string MaterialNumber, string SerialNumber, CancellationToken cancellationToken)
		{
			var result = await _inboundStockService.DeleteSerialAsync(MaterialNumber, SerialNumber, cancellationToken);

			return Deleted(result, "Serial number deleted successfully.");
		}

		[HttpPost("serialNumberHardDelete/{MaterialNumber}/{SerialNumber}")]
		public async Task<IActionResult> serialNumberHardDelete(string MaterialNumber, string SerialNumber, CancellationToken cancellationToken)
		{
			var result = await _inboundStockService.HardDeleteSerialAsync(MaterialNumber, SerialNumber, cancellationToken);

			return Deleted(result, "Serial number permanently deleted successfully.");
		}

		[HttpPost("UpdateSerialStatus/{MaterialNumber}/{SerialNumber}/{status}/{UserName}")]
		public async Task<IActionResult> UpdateSerialStatus(string MaterialNumber, string SerialNumber, string status, string UserName, CancellationToken cancellationToken)
		{
			await _inboundStockService.UpdateSerialStatusAsync(MaterialNumber, SerialNumber, status, UserName, cancellationToken);

			return Updated(message: "Serial status updated successfully.");
		}

		// ------------------------------------------------------------------ Entity CRUD

		// PUT: api/SmInboundStockCiis/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutSmInboundStockCii(string id, SmInboundStockCii smInboundStockCii, CancellationToken cancellationToken)
		{
			await _inboundStockService.UpdateAsync(id, smInboundStockCii, cancellationToken);

			return Updated(message: "Inbound CII stock updated successfully.");
		}

		// POST: api/SmInboundStockCiis
		[HttpPost]
		public async Task<IActionResult> PostSmInboundStockCii(SmInboundStockCii smInboundStockCii, CancellationToken cancellationToken)
		{
			var created = await _inboundStockService.CreateAsync(smInboundStockCii, cancellationToken);

			return Created(created, "Inbound CII stock created successfully.");
		}

		// DELETE: api/SmInboundStockCiis/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSmInboundStockCii(string id, CancellationToken cancellationToken)
		{
			await _inboundStockService.DeleteAsync(id, cancellationToken);

			return Deleted(message: "Inbound CII stock deleted successfully.");
		}
	}
}
