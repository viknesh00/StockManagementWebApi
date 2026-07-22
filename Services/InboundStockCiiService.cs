using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Common.Files;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.NonStockCII;
using StockManagementWebApi.Models.Notifications;

namespace StockManagementWebApi.Services
{
	public interface IInboundStockCiiService
	{
		Task<IReadOnlyList<StockCiiList>> GetStockListAsync(string userName, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<ReportCii>> GetReportAsync(string userName, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<Log_record>> GetLogRecordsAsync(CancellationToken cancellationToken = default);

		Task<IReadOnlyList<MaterialComparisonResult>> CompareMaterialsAsync(ExcelCompareRequest data, CancellationToken cancellationToken = default);

		Task ImportBulkMaterialStockAsync(AddStockInward data, CancellationToken cancellationToken = default);

		Task ImportStockDataAsync(AddStockInward data, CancellationToken cancellationToken = default);

		Task ImportSingleStockAsync(AddSingleStockInward data, CancellationToken cancellationToken = default);

		Task<UpdateInboundData?> UpdateInboundDataAsync(List<UpdateInboundData> data, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<StockInboundCIIList>> GetOverallStockAsync(string userName, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<StockInboundCIIList>> SearchBySerialNumberAsync(string userName, string serialNumber, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<StockInboundCIIList>> GetByMaterialAndSerialAsync(string materialNumber, string? serialNumber, string userName, CancellationToken cancellationToken = default);

		Task AddMaterialAsync(AddMaterial data, CancellationToken cancellationToken = default);

		Task UpdateMaterialAsync(UpdateAddMaterial data, CancellationToken cancellationToken = default);

		Task DeleteMaterialAsync(string materialNumber, bool isActive, string userName, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<SmInboundStockCiiDelete>> DeleteSerialAsync(string materialNumber, string serialNumber, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<SmInboundStockCiiDelete>> HardDeleteSerialAsync(string materialNumber, string serialNumber, CancellationToken cancellationToken = default);

		Task UpdateSerialStatusAsync(string materialNumber, string serialNumber, string status, string userName, CancellationToken cancellationToken = default);

		Task UpdateAsync(string id, SmInboundStockCii stock, CancellationToken cancellationToken = default);

		Task<SmInboundStockCii> CreateAsync(SmInboundStockCii stock, CancellationToken cancellationToken = default);

		Task DeleteAsync(string id, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Inbound CII stock: listings, Excel imports, material master maintenance and serial-number
	/// operations. Every database call is awaited, and every Excel import stages its upload
	/// through <see cref="IUploadedFileStore"/> so the temporary file is always removed.
	/// </summary>
	public class InboundStockCiiService : IInboundStockCiiService
	{
		private const string DuplicateSerialMessage = "Duplicate entry: The Serial number already exists.";

		private readonly MydbContext _context;
		private readonly IUploadedFileStore _fileStore;
		private readonly INotificationPublisher _notifications;
		private readonly ILogger<InboundStockCiiService> _logger;

		public InboundStockCiiService(
			MydbContext context,
			IUploadedFileStore fileStore,
			INotificationPublisher notifications,
			ILogger<InboundStockCiiService> logger)
		{
			_context = context;
			_fileStore = fileStore;
			_notifications = notifications;
			_logger = logger;
		}

		// ------------------------------------------------------------------ Listings

		public async Task<IReadOnlyList<StockCiiList>> GetStockListAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return await _context.StockCiiLists
				.FromSqlRaw(@"exec StockCIIList @p0", userName)
				.ToListAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<ReportCii>> GetReportAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return await _context.ReportCiis
				.FromSqlRaw(@"exec ReportCIIList @p0", userName)
				.ToListAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<Log_record>> GetLogRecordsAsync(CancellationToken cancellationToken = default)
			=> await _context.Log_records.FromSqlRaw("SELECT * FROM Log_record").ToListAsync(cancellationToken);

		public async Task<IReadOnlyList<StockInboundCIIList>> GetOverallStockAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return await _context.StockInboundCIILists
				.FromSqlRaw(@"exec Get_OverallCiiStock @p0", userName)
				.ToListAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<StockInboundCIIList>> SearchBySerialNumberAsync(string userName, string serialNumber, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));
			Require(serialNumber, nameof(serialNumber));

			return await _context.StockInboundCIILists
				.FromSqlRaw(@"exec searchbyserialnumber @p0 , @p1", serialNumber, userName)
				.ToListAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<StockInboundCIIList>> GetByMaterialAndSerialAsync(string materialNumber, string? serialNumber, string userName, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(userName, nameof(userName));

			// The route serialises an absent serial number as the literal text "null".
			var normalisedSerial =
				string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Equals("null", StringComparison.OrdinalIgnoreCase)
					? null
					: serialNumber;

			return await _context.StockInboundCIILists
				.FromSqlRaw(@"EXEC StockCIIListBySerialNumber @p0, @p1, @p2", materialNumber, normalisedSerial, userName)
				.ToListAsync(cancellationToken);
		}

		// ------------------------------------------------------------------ Excel comparison

		public async Task<IReadOnlyList<MaterialComparisonResult>> CompareMaterialsAsync(ExcelCompareRequest data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var filePath = await _fileStore.SaveAsync(data.file, cancellationToken);

			try
			{
				var excelData = new List<(string PoolName, string MaterialNumber, int ExcelStatus)>();

				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
				using (var package = new ExcelPackage(new FileInfo(filePath)))
				{
					var worksheet = package.Workbook.Worksheets[0];
					if (worksheet.Dimension == null)
					{
						throw new BadRequestException("The Excel file contains no data.");
					}

					var rowCount = worksheet.Dimension.Rows;
					for (var row = 2; row <= rowCount; row++)
					{
						var poolName = worksheet.Cells[row, 2].Text?.Trim() ?? string.Empty;
						var materialNumber = worksheet.Cells[row, 3].Text?.Trim() ?? string.Empty;
						int.TryParse(worksheet.Cells[row, 12].Text?.Trim(), out var excelStatus);

						if (!string.IsNullOrEmpty(materialNumber))
						{
							excelData.Add((poolName, materialNumber, excelStatus));
						}
					}
				}

				// 1. Build a distinct, comma-separated material list for the stored procedure.
				var distinctMaterialNumbers = excelData.Select(x => x.MaterialNumber).Distinct();
				var materialParam = string.Join(",", distinctMaterialNumbers.Select(m => $"'{m}'"));

				// 2. Query the database once.
				var dbData = await _context.StockCiiLists
					.FromSqlRaw("EXEC MaterialCIIListBulk @p0, @p1", data.UserName, materialParam)
					.ToListAsync(cancellationToken);

				// 3. Build a lookup dictionary.
				var dbLookup = dbData.ToDictionary(x => x.materialNumber, StringComparer.OrdinalIgnoreCase);

				// 4. Match the spreadsheet rows against it.
				var resultList = new List<MaterialComparisonResult>(excelData.Count);
				foreach (var row in excelData)
				{
					dbLookup.TryGetValue(row.MaterialNumber, out var dbItem);

					resultList.Add(new MaterialComparisonResult
					{
						PoolName = row.PoolName,
						ExcelMaterialNumber = row.MaterialNumber,
						ExcelStatus = row.ExcelStatus,
						DbMaterialNumber = dbItem?.materialNumber,
						newstock = dbItem?.newstock,
						usedstock = dbItem?.usedstock,
						Damaged = dbItem?.Damaged,
						BreakFix = dbItem?.BreakFix
					});
				}

				return resultList;
			}
			finally
			{
				_fileStore.TryDelete(filePath);
			}
		}

		// ------------------------------------------------------------------ Imports

		public async Task ImportBulkMaterialStockAsync(AddStockInward data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var userCode = await ResolveUserCodeAsync(data.UserName, cancellationToken);
			var filePath = await _fileStore.SaveAsync(data.file, cancellationToken);

			try
			{
				var inboundStocks = ReadBulkMaterialRows(filePath);
				if (inboundStocks.Count == 0)
				{
					throw new BadRequestException("The Excel file contains no data.");
				}

				var tenantCode = await _context.Database
					.SqlQuery<string>($"SELECT Fk_TenentCode AS Value FROM [dbo].[sm_Users] WHERE LoginId = {data.UserName}")
					.FirstOrDefaultAsync(cancellationToken);

				foreach (var stock in inboundStocks)
				{
					var materialNumber = stock["MaterialNumber"];

					var isMaterialNumberAvailable = await _context.Database
						.SqlQuery<int>($@"
SELECT 1 AS Value
FROM [dbo].[sm_material_master] smm
INNER JOIN [dbo].[sm_Users] su ON su.Pk_UserCode = smm.Fk_UserCode
WHERE smm.MaterialNumber = {materialNumber}
  AND su.Fk_TenentCode = {tenantCode}")
						.AnyAsync(cancellationToken);

					if (!isMaterialNumberAvailable)
					{
						await _context.Database.ExecuteSqlRawAsync(
							@"EXEC AddMaterialNumberNew @p0, @p1, @p2",
							new object?[] { data.UserName, materialNumber, stock["MaterialDescription"] }!,
							cancellationToken);
					}

					await ExecuteAddSingleStockAsync(
						data.UserName, data.DeliveryNumber, data.OrderNumber,
						materialNumber, stock["MaterialDescription"], stock["SerialNumber"],
						stock["Quantity"], data.Inwarddate, data.InwardFrom, data.ReceivedBy,
						stock["Status"], userCode, data.RacKLocation, data.PoNumber, data.Location,
						cancellationToken);
				}

				_logger.LogInformation(
					"Bulk material stock import completed for {UserName}: {RowCount} row(s).",
					data.UserName, inboundStocks.Count);

				// One summary notification per import, not one per row - a 500-line spreadsheet
				// should not produce 500 notifications.
				await _notifications.PublishAsync(
					data.UserName,
					NotificationTypes.StockInward,
					NotificationSeverities.Success,
					"Bulk CII stock inwarded",
					$"{inboundStocks.Count} CII item(s) were inwarded against delivery {data.DeliveryNumber}.",
					referenceType: "CiiInward",
					referenceId: data.DeliveryNumber,
					cancellationToken: cancellationToken);
			}
			finally
			{
				_fileStore.TryDelete(filePath);
			}
		}

		public async Task ImportStockDataAsync(AddStockInward data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var userCode = await ResolveUserCodeAsync(data.UserName, cancellationToken);
			var filePath = await _fileStore.SaveAsync(data.file, cancellationToken);

			try
			{
				var inboundStocks = ReadSerialRows(filePath);
				if (inboundStocks.Count == 0)
				{
					throw new BadRequestException("The Excel file contains no data.");
				}

				foreach (var stock in inboundStocks)
				{
					await ExecuteAddSingleStockAsync(
						data.UserName, data.DeliveryNumber, data.OrderNumber,
						data.MaterialNumber, data.MaterialDescription, stock["SerialNumber"],
						stock["Quantity"], data.Inwarddate, data.InwardFrom, data.ReceivedBy,
						stock["Status"], userCode, data.RacKLocation, data.PoNumber, data.Location,
						cancellationToken);
				}

				_logger.LogInformation(
					"Stock import completed for {UserName}: {RowCount} serial number(s).",
					data.UserName, inboundStocks.Count);

				await _notifications.PublishAsync(
					data.UserName,
					NotificationTypes.StockInward,
					NotificationSeverities.Success,
					"CII stock inwarded",
					$"{inboundStocks.Count} serial number(s) of material {data.MaterialNumber} were inwarded against delivery {data.DeliveryNumber}.",
					materialNumber: data.MaterialNumber,
					referenceType: "CiiInward",
					referenceId: data.DeliveryNumber,
					cancellationToken: cancellationToken);
			}
			finally
			{
				_fileStore.TryDelete(filePath);
			}
		}

		public async Task ImportSingleStockAsync(AddSingleStockInward data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var userCode = await ResolveUserCodeAsync(data.UserName, cancellationToken);

			await ExecuteAddSingleStockAsync(
				data.UserName, data.DeliveryNumber, data.OrderNumber,
				data.MaterialNumber, data.MaterialDescription, data.SerialNumber,
				data.Quantity, data.Inwarddate, data.InwardFrom, data.ReceivedBy,
				data.Status, userCode, data.RacKLocation, data.PoNumber, data.Location,
				cancellationToken);

			_logger.LogInformation(
				"Serial number {SerialNumber} of material {MaterialNumber} inwarded by {UserName}.",
				data.SerialNumber, data.MaterialNumber, data.UserName);

			await _notifications.PublishAsync(
				data.UserName,
				NotificationTypes.StockInward,
				NotificationSeverities.Success,
				"CII stock inwarded",
				$"Serial number {data.SerialNumber} of material {data.MaterialNumber} was inwarded.",
				materialNumber: data.MaterialNumber,
				serialNumber: data.SerialNumber,
				referenceType: "CiiInward",
				referenceId: data.DeliveryNumber,
				cancellationToken: cancellationToken);
		}

		// ------------------------------------------------------------------ Inbound updates

		/// <remarks>
		/// Behaviour preserved verbatim from the original controller: the loop returns after the
		/// first element, so only the first row of the payload is applied and that row is echoed
		/// back. An empty payload is a no-op. This is intentionally left as-is - changing it
		/// would alter how existing bulk edits behave.
		/// </remarks>
		public async Task<UpdateInboundData?> UpdateInboundDataAsync(List<UpdateInboundData> data, CancellationToken cancellationToken = default)
		{
			if (data == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			foreach (var item in data)
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"exec updateInboundStockCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12",
					new object?[]
					{
						item.userName, item.MaterialNumber, item.SerialNumber, item.ExistSerialNumber,
						item.RackLocation, item.DeliveryNumber, item.OrderNumber, item.InwardDate,
						item.InwardFrom, item.ReceivedBy, item.QualityChecker, item.QualityCheckerStatus,
						item.QualityCheckDate
					}!,
					cancellationToken);

				_logger.LogInformation(
					"Inbound CII record updated for material {MaterialNumber}, serial {SerialNumber}.",
					item.MaterialNumber, item.SerialNumber);

				return item;
			}

			return null;
		}

		// ------------------------------------------------------------------ Material master

		public async Task AddMaterialAsync(AddMaterial data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			try
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"exec AddMaterialNumberNew @p0, @p1, @p2",
					new object?[] { data.userName, data.MaterialNumber, data.MaterialDescription }!,
					cancellationToken);
			}
			catch (SqlException exception) when (exception.Number == SqlErrorCodes.ApplicationDuplicateKey)
			{
				throw new BusinessException(
					StatusCodes.Status400BadRequest,
					"Duplicate entry: The material number already exists.",
					innerException: exception);
			}

			_logger.LogInformation("Material {MaterialNumber} created by {UserName}.", data.MaterialNumber, data.userName);
		}

		public async Task UpdateMaterialAsync(UpdateAddMaterial data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec updatematerialNumber @p0, @p1, @p2, @p3",
				new object?[] { data.userName, data.ExistMaterialNumber, data.MaterialNumber, data.MaterialDescription }!,
				cancellationToken);

			_logger.LogInformation("Material {MaterialNumber} updated by {UserName}.", data.MaterialNumber, data.userName);
		}

		public async Task DeleteMaterialAsync(string materialNumber, bool isActive, string userName, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(userName, nameof(userName));

			await _context.Database.ExecuteSqlRawAsync(
				@"exec deletematerialnumber @p0, @p1, @p2",
				new object[] { userName, materialNumber, isActive },
				cancellationToken);

			_logger.LogInformation(
				"Material {MaterialNumber} active flag set to {IsActive} by {UserName}.",
				materialNumber, isActive, userName);
		}

		// ------------------------------------------------------------------ Serial numbers

		public async Task<IReadOnlyList<SmInboundStockCiiDelete>> DeleteSerialAsync(string materialNumber, string serialNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(serialNumber, nameof(serialNumber));

			var result = await _context.SmInboundStockCiiDeletes
				.FromSqlRaw(@"exec sp_deleteserialNumber @p0, @p1", materialNumber, serialNumber)
				.ToListAsync(cancellationToken);

			_logger.LogInformation(
				"Soft delete requested for serial {SerialNumber} of material {MaterialNumber}.",
				serialNumber, materialNumber);

			return result;
		}

		public async Task<IReadOnlyList<SmInboundStockCiiDelete>> HardDeleteSerialAsync(string materialNumber, string serialNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(serialNumber, nameof(serialNumber));

			var result = await _context.SmInboundStockCiiDeletes
				.FromSqlRaw(@"exec sp_deleteHardserialNumber @p0, @p1", materialNumber, serialNumber)
				.ToListAsync(cancellationToken);

			_logger.LogWarning(
				"Hard delete performed for serial {SerialNumber} of material {MaterialNumber}.",
				serialNumber, materialNumber);

			// Irreversible, so it is worth surfacing to the whole tenant.
			await _notifications.PublishAsync(
				null, // route carries no user; the publisher uses the token identity
				NotificationTypes.StockDeleted,
				NotificationSeverities.Warning,
				"Serial number permanently deleted",
				$"Serial number {serialNumber} of material {materialNumber} was permanently deleted.",
				materialNumber: materialNumber,
				serialNumber: serialNumber,
				referenceType: "CiiSerial",
				referenceId: serialNumber,
				cancellationToken: cancellationToken);

			return result;
		}

		public async Task UpdateSerialStatusAsync(string materialNumber, string serialNumber, string status, string userName, CancellationToken cancellationToken = default)
		{
			var errors = new List<string>();
			if (string.IsNullOrWhiteSpace(materialNumber))
			{
				errors.Add("MaterialNumber cannot be empty.");
			}

			if (string.IsNullOrWhiteSpace(serialNumber))
			{
				errors.Add("SerialNumber cannot be empty.");
			}

			if (string.IsNullOrWhiteSpace(status))
			{
				errors.Add("status cannot be empty.");
			}

			if (errors.Count > 0)
			{
				throw new ValidationException(errors, "MaterialNumber, SerialNumber, and status cannot be empty.");
			}

			var currentStatus = await _context.Database
				.SqlQueryRaw<string>(
					"SELECT status FROM [dbo].[sm_Inbound_StockCII] WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
					serialNumber, materialNumber)
				.ToListAsync(cancellationToken);

			// The original indexed [0] unconditionally, which threw for an unknown serial number.
			if (currentStatus.Count == 0)
			{
				throw new NotFoundException($"Serial number '{serialNumber}' was not found for material '{materialNumber}'.");
			}

			if (string.Equals(currentStatus[0], "Outward", StringComparison.OrdinalIgnoreCase))
			{
				throw new BusinessException(
					StatusCodes.Status400BadRequest,
					"The serial number status should not be 'Outward' before Update.");
			}

			await _context.Database.ExecuteSqlRawAsync(
				"EXEC sp_updateserialstatus @p0, @p1, @p2,@p3",
				new object[] { userName, materialNumber, serialNumber, status },
				cancellationToken);

			_logger.LogInformation(
				"Serial {SerialNumber} of material {MaterialNumber} moved to status {Status} by {UserName}.",
				serialNumber, materialNumber, status, userName);
		}

		// ------------------------------------------------------------------ Entity CRUD

		public async Task UpdateAsync(string id, SmInboundStockCii stock, CancellationToken cancellationToken = default)
		{
			if (stock == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			if (id != stock.DeliveryNumber)
			{
				throw new ValidationException("The delivery number in the URL does not match the delivery number in the body.");
			}

			_context.Entry(stock).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateConcurrencyException exception)
			{
				if (!await ExistsAsync(id, cancellationToken))
				{
					throw new NotFoundException($"Inbound CII stock '{id}' was not found.", innerException: exception);
				}

				throw;
			}
		}

		public async Task<SmInboundStockCii> CreateAsync(SmInboundStockCii stock, CancellationToken cancellationToken = default)
		{
			if (stock == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			_context.SmInboundStockCiis.Add(stock);

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateException exception)
			{
				if (await ExistsAsync(stock.DeliveryNumber, cancellationToken))
				{
					throw new ConflictException($"Inbound CII stock '{stock.DeliveryNumber}' already exists.", innerException: exception);
				}

				throw;
			}

			return stock;
		}

		public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var stock = await _context.SmInboundStockCiis.FindAsync(new object?[] { id }, cancellationToken)
				?? throw NotFoundException.For("Inbound CII stock", id);

			_context.SmInboundStockCiis.Remove(stock);
			await _context.SaveChangesAsync(cancellationToken);
		}

		// ------------------------------------------------------------------ Helpers

		/// <summary>
		/// Runs Addsinglestock, translating a duplicate-key failure into the message the API has
		/// always returned for this case.
		/// </summary>
		private async Task ExecuteAddSingleStockAsync(
			object? userName, object? deliveryNumber, object? orderNumber, object? materialNumber,
			object? materialDescription, object? serialNumber, object? quantity, object? inwardDate,
			object? inwardFrom, object? receivedBy, object? status, object? userCode,
			object? rackLocation, object? poNumber, object? location,
			CancellationToken cancellationToken)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"exec Addsinglestock @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10 ,@p11,@p12,@p13,@p14",
					new object?[]
					{
						userName, deliveryNumber, orderNumber, materialNumber, materialDescription,
						serialNumber, quantity, inwardDate, inwardFrom, receivedBy, status,
						userCode, rackLocation, poNumber, location
					}!,
					cancellationToken);
			}
			catch (SqlException exception) when (exception.IsDuplicateKey())
			{
				throw new BusinessException(StatusCodes.Status400BadRequest, DuplicateSerialMessage, innerException: exception);
			}
		}

		private async Task<string> ResolveUserCodeAsync(string? userName, CancellationToken cancellationToken)
		{
			var userCodes = await _context.Database
				.SqlQueryRaw<string>("SELECT Pk_UserCode FROM sm_users WHERE loginId = @p0", userName)
				.ToListAsync(cancellationToken);

			// The original indexed [0] directly, which threw for an unknown login.
			if (userCodes.Count == 0 || string.IsNullOrEmpty(userCodes[0]))
			{
				throw new BadRequestException("User not found.");
			}

			return userCodes[0];
		}

		private static List<Dictionary<string, object>> ReadBulkMaterialRows(string filePath)
		{
			var rows = new List<Dictionary<string, object>>();

			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			using var package = new ExcelPackage(new FileInfo(filePath));
			var worksheet = package.Workbook.Worksheets[0];
			if (worksheet.Dimension == null)
			{
				return rows;
			}

			var rowCount = worksheet.Dimension.Rows;
			for (var row = 2; row <= rowCount; row++)
			{
				if (worksheet.Cells[row, 1].Text == "")
				{
					continue;
				}

				rows.Add(new Dictionary<string, object>
				{
					{ "MaterialNumber", worksheet.Cells[row, 1].Text },
					{ "MaterialDescription", worksheet.Cells[row, 2].Text },
					{ "SerialNumber", worksheet.Cells[row, 3].Text },
					{ "Quantity", int.TryParse(worksheet.Cells[row, 4].Text, out var qty) ? qty : 0 },
					{ "Status", worksheet.Cells[row, 5].Text }
				});
			}

			return rows;
		}

		private static List<Dictionary<string, object>> ReadSerialRows(string filePath)
		{
			var rows = new List<Dictionary<string, object>>();

			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			using var package = new ExcelPackage(new FileInfo(filePath));
			var worksheet = package.Workbook.Worksheets[0];
			if (worksheet.Dimension == null)
			{
				return rows;
			}

			var rowCount = worksheet.Dimension.Rows;
			for (var row = 2; row <= rowCount; row++)
			{
				if (worksheet.Cells[row, 1].Text == "")
				{
					continue;
				}

				rows.Add(new Dictionary<string, object>
				{
					{ "SerialNumber", worksheet.Cells[row, 1].Text },
					{ "Quantity", int.TryParse(worksheet.Cells[row, 2].Text, out var qty) ? qty : 0 },
					{ "Status", worksheet.Cells[row, 3].Text }
				});
			}

			return rows;
		}

		private Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
			=> _context.SmInboundStockCiis.AsNoTracking().AnyAsync(e => e.DeliveryNumber == id, cancellationToken);

		private static void Require(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ValidationException($"{parameterName} is required.");
			}
		}

		private static void RequireBody<T>(T? data) where T : class
		{
			if (data == null)
			{
				throw new BadRequestException("Invalid request data.");
			}
		}
	}
}
