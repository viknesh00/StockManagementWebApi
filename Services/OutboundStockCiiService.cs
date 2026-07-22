using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.Responses;

namespace StockManagementWebApi.Services
{
	public interface IOutboundStockCiiService
	{
		Task<IReadOnlyList<SmOutboundStockCii>> GetAllAsync(CancellationToken cancellationToken = default);

		Task<SmOutboundStockCii> GetByIdAsync(string id, CancellationToken cancellationToken = default);

		Task UpdateCollectionPointAsync(CollectionPointDetail data, CancellationToken cancellationToken = default);

		Task<OutboundStockLookupResult> GetStockLookupAsync(string materialNumber, string serialNumber, string orderNumber, CancellationToken cancellationToken = default);

		Task AddOutboundDataAsync(AddDeliveryData data, CancellationToken cancellationToken = default);

		Task AddBulkOutboundDataAsync(BulkAddDeliveryData data, CancellationToken cancellationToken = default);

		Task DeleteOutboundDataAsync(string materialNumber, string serialNumber, int outboundStockCiiKey, CancellationToken cancellationToken = default);

		Task UpdateDeliveryDataAsync(UpdatedeliveryDataList data, CancellationToken cancellationToken = default);

		Task AddReturnDataAsync(AddReturnDataList data, CancellationToken cancellationToken = default);

		Task UpdateReturnDataAsync(UpdateReturnDataList data, CancellationToken cancellationToken = default);

		Task DeleteReturnDataAsync(string materialNumber, string serialNumber, int returnStockCiiKey, CancellationToken cancellationToken = default);

		Task AddStagingAsync(StagingModel data, CancellationToken cancellationToken = default);

		Task UpdateStagingAsync(StagingModel data, CancellationToken cancellationToken = default);

		Task DeleteStagingAsync(int id, CancellationToken cancellationToken = default);

		Task UpdateAsync(string id, SmOutboundStockCii stock, CancellationToken cancellationToken = default);

		Task<SmOutboundStockCii> CreateAsync(SmOutboundStockCii stock, CancellationToken cancellationToken = default);

		Task DeleteAsync(string id, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Outbound CII stock: deliveries, returns, collection points and staging. The multi-step
	/// delete operations run inside an explicit transaction so a mid-way failure leaves no
	/// half-applied state.
	/// </summary>
	public class OutboundStockCiiService : IOutboundStockCiiService
	{
		/// <summary>Statuses that mean a serial number has already left inbound stock.</summary>
		private static readonly string[] AlreadyProcessedStatuses = { "Outward", "Defective", "Damaged", "BreakFix" };

		private readonly MydbContext _context;
		private readonly ILogger<OutboundStockCiiService> _logger;

		public OutboundStockCiiService(MydbContext context, ILogger<OutboundStockCiiService> logger)
		{
			_context = context;
			_logger = logger;
		}

		// ------------------------------------------------------------------ Listings

		public async Task<IReadOnlyList<SmOutboundStockCii>> GetAllAsync(CancellationToken cancellationToken = default)
			=> await _context.SmOutboundStockCiis.ToListAsync(cancellationToken);

		public async Task<SmOutboundStockCii> GetByIdAsync(string id, CancellationToken cancellationToken = default)
		{
			var stock = await _context.SmOutboundStockCiis.FindAsync(new object?[] { id }, cancellationToken);

			return stock ?? throw NotFoundException.For("Outbound CII stock", id);
		}

		public async Task<OutboundStockLookupResult> GetStockLookupAsync(string materialNumber, string serialNumber, string orderNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(serialNumber, nameof(serialNumber));

			return new OutboundStockLookupResult
			{
				CIIData = await _context.InboundCIILists
					.FromSqlRaw(@"exec sp_inboundstockList @p0, @p1, @p2", materialNumber, serialNumber, orderNumber)
					.ToListAsync(cancellationToken),
				InboundData = await _context.ReturnStockDatas
					.FromSqlRaw(@"exec deliverystockCII @p0,@p1", materialNumber, serialNumber)
					.ToListAsync(cancellationToken),
				DeliveryData = await _context.OutboundDataLists
					.FromSqlRaw(@"exec sp_outboundstockList @p0, @p1", materialNumber, serialNumber)
					.ToListAsync(cancellationToken),
				StagingData = await _context.StagingStockDatas
					.FromSqlRaw(@"exec stagingstockCII @p0,@p1", materialNumber, serialNumber)
					.ToListAsync(cancellationToken)
			};
		}

		// ------------------------------------------------------------------ Collection point

		public async Task UpdateCollectionPointAsync(CollectionPointDetail data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			const string sql = @"EXEC UpdateCollectionPointDetails
				   @username = {0},
		           @MaterialNumber = {1},
		           @SerialNumber = {2},
		           @CollectionPointStatus = {3},
		           @CollectionPointDate = {4},
		           @CollectionPointerName = {5},
                   @RackLocation={6}";

			await _context.Database.ExecuteSqlRawAsync(
				sql,
				new object?[]
				{
					data.UserName, data.MaterialNumber, data.SerialNumber, data.CollectionPointStatus,
					data.CollectionPointDate, data.CollectionPointerName, data.RackLocation
				}!,
				cancellationToken);

			_logger.LogInformation(
				"Collection point updated for material {MaterialNumber}, serial {SerialNumber}.",
				data.MaterialNumber, data.SerialNumber);
		}

		// ------------------------------------------------------------------ Outward

		public async Task AddOutboundDataAsync(AddDeliveryData data, CancellationToken cancellationToken = default)
		{
			if (data == null || data.SerialNumber == null || data.SerialNumber.Count == 0)
			{
				throw new ValidationException("At least one SerialNumber is required.");
			}

			var serialList = data.SerialNumber;
			var deliveryList = data.Fk_Inbound_StockCII_DeliveryNumber;

			if (deliveryList != null && serialList.Count != deliveryList.Count)
			{
				throw new ValidationException("SerialNumber and DeliveryNumber count mismatch.");
			}

			for (var i = 0; i < serialList.Count; i++)
			{
				var serial = serialList[i];
				var deliveryNumber = deliveryList != null ? deliveryList[i] : data.DeliveryNumber;

				await GuardSerialNotAlreadyProcessedAsync(serial, data.MaterialNumber, cancellationToken);

				await _context.Database.ExecuteSqlRawAsync(
					"EXEC AddInboundStockCII @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10",
					new object?[]
					{
						data.UserName, data.DeliveryNumber, data.MaterialNumber, serial,
						data.MaterialDescription, data.OrderNumber, data.OutBounddate,
						data.TargetLocation, data.SentBy, deliveryNumber, data.ReceiverName
					}!,
					cancellationToken);

				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Inbound_StockCII SET Status = 'Outward' WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
					new object?[] { serial, data.MaterialNumber }!,
					cancellationToken);
			}

			_logger.LogInformation(
				"Outward recorded for {SerialCount} serial number(s) of material {MaterialNumber} by {UserName}.",
				serialList.Count, data.MaterialNumber, data.UserName);
		}

		public async Task AddBulkOutboundDataAsync(BulkAddDeliveryData data, CancellationToken cancellationToken = default)
		{
			if (data == null || data.SerialNumber == null || data.SerialNumber.Count == 0)
			{
				throw new ValidationException("At least one SerialNumber is required.");
			}

			var serialList = data.SerialNumber;
			var deliveryList = data.Fk_Inbound_StockCII_DeliveryNumber;
			var materialNumberList = data.MaterialNumber;
			var materialDescriptionList = data.MaterialDescription;

			if (materialNumberList == null || materialNumberList.Count != serialList.Count)
			{
				throw new ValidationException("MaterialNumber and SerialNumber count mismatch.");
			}

			if (materialDescriptionList == null || materialDescriptionList.Count != serialList.Count)
			{
				throw new ValidationException("MaterialDescription and SerialNumber count mismatch.");
			}

			if (deliveryList != null && deliveryList.Count != serialList.Count)
			{
				throw new ValidationException("SerialNumber and DeliveryNumber count mismatch.");
			}

			for (var i = 0; i < serialList.Count; i++)
			{
				var serial = serialList[i];
				var materialNumber = materialNumberList[i];
				var materialDescription = materialDescriptionList[i];
				var deliveryNumber = deliveryList != null ? deliveryList[i] : data.DeliveryNumber;

				await GuardSerialNotAlreadyProcessedAsync(serial, materialNumber, cancellationToken);

				await _context.Database.ExecuteSqlRawAsync(
					@"EXEC BulkAddInboundStockCII
                    @p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12",
					new object?[]
					{
						data.UserName, data.DeliveryNumber, materialNumber, serial, materialDescription,
						data.OrderNumber, data.OutBounddate, data.TargetLocation, data.SentBy,
						deliveryNumber, data.ReceiverName, data.Status, data.SubStatus
					}!,
					cancellationToken);

				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Inbound_StockCII SET Status = 'Outward' WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
					new object?[] { serial, materialNumber }!,
					cancellationToken);
			}

			_logger.LogInformation(
				"Bulk outward recorded for {SerialCount} serial number(s) by {UserName}.",
				serialList.Count, data.UserName);
		}

		public async Task DeleteOutboundDataAsync(string materialNumber, string serialNumber, int outboundStockCiiKey, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(serialNumber, nameof(serialNumber));

			// Disposing the transaction without a commit rolls it back, so both the exception
			// paths below and any failure inside the block leave the data untouched.
			await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

			var deletedRows = await _context.Database.ExecuteSqlRawAsync(
				@"DELETE FROM sm_Outbound_StockCII
              WHERE serialNumber = @p0
              AND materialNumber = @p1
              AND OutBoundStockCIIKey = @p2",
				new object[] { serialNumber, materialNumber, outboundStockCiiKey },
				cancellationToken);

			if (deletedRows == 0)
			{
				throw new NotFoundException("No outbound data found to delete.");
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"UPDATE sm_Inbound_StockCII
              SET Status = 'New'
              WHERE serialNumber = @p0
              AND materialNumber = @p1",
				new object[] { serialNumber, materialNumber },
				cancellationToken);

			await transaction.CommitAsync(cancellationToken);

			_logger.LogInformation(
				"Outbound record {Key} deleted for material {MaterialNumber}, serial {SerialNumber}.",
				outboundStockCiiKey, materialNumber, serialNumber);
		}

		public async Task UpdateDeliveryDataAsync(UpdatedeliveryDataList data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec updatedeliverydata @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8",
				new object?[]
				{
					data.UserName, data.MaterialNumber, data.SerialNumber, data.OrderNumber,
					data.ExistOrderNumber, data.Outbounddate, data.TargetLocation, data.SentBy,
					data.ReceiverName
				}!,
				cancellationToken);

			_logger.LogInformation(
				"Delivery data updated for material {MaterialNumber}, serial {SerialNumber}.",
				data.MaterialNumber, data.SerialNumber);
		}

		// ------------------------------------------------------------------ Returns

		public async Task AddReturnDataAsync(AddReturnDataList data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var status = await _context.Database
				.SqlQueryRaw<string>(
					"SELECT status FROM [dbo].[sm_Inbound_StockCII] WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
					data.SerialNumber, data.MaterialNumber)
				.ToListAsync(cancellationToken);

			// The original indexed [0] before the emptiness check, which threw for an unknown
			// serial number instead of producing the intended 404.
			if (status.Count == 0 || string.IsNullOrEmpty(status[0]))
			{
				throw new NotFoundException("Serial number or material number not found.");
			}

			if (!string.Equals(status[0], "Outward", StringComparison.OrdinalIgnoreCase))
			{
				throw new BusinessException(
					StatusCodes.Status400BadRequest,
					"The serial number status should be 'Outward' before returning.");
			}

			await _context.Database.ExecuteSqlRawAsync(
				"EXEC AddReturnStockCII @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10,@p11",
				new object?[]
				{
					data.UserName, data.DeliveryNumber, data.MaterialNumber, data.MaterialDescription,
					data.SerialNumber, data.OrderNumber, data.LocationReturnedFrom, data.Returneddate,
					data.ReturnedBy, data.RackLocation, data.ReturnType, data.Returns
				}!,
				cancellationToken);

			await _context.Database.ExecuteSqlRawAsync(
				"UPDATE sm_Inbound_StockCII SET Status = @p0 WHERE SerialNumber = @p1 AND MaterialNumber = @p2",
				new object?[] { data.ReturnType, data.SerialNumber, data.MaterialNumber }!,
				cancellationToken);

			_logger.LogInformation(
				"Return recorded for material {MaterialNumber}, serial {SerialNumber} as {ReturnType}.",
				data.MaterialNumber, data.SerialNumber, data.ReturnType);
		}

		public async Task UpdateReturnDataAsync(UpdateReturnDataList data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec UpdateReturndata @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10",
				new object?[]
				{
					data.UserName, data.MaterialNumber, data.SerialNumber, data.OrderNumber,
					data.LocationReturnedFrom, data.ReturnedDate, data.RackLocation, data.ReturnType,
					data.ReturnedBy, data.Returns, data.ExistOrderNumber
				}!,
				cancellationToken);

			_logger.LogInformation(
				"Return data updated for material {MaterialNumber}, serial {SerialNumber}.",
				data.MaterialNumber, data.SerialNumber);
		}

		public async Task DeleteReturnDataAsync(string materialNumber, string serialNumber, int returnStockCiiKey, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(serialNumber, nameof(serialNumber));

			await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

			var deletedRows = await _context.Database.ExecuteSqlRawAsync(
				@"DELETE FROM sm_ReturnStock_CII
              WHERE SerialNumber = @p0
              AND MaterialNumber = @p1
              AND ReturnStockCIIKey = @p2",
				new object[] { serialNumber, materialNumber, returnStockCiiKey },
				cancellationToken);

			if (deletedRows == 0)
			{
				throw new NotFoundException("No return data found to delete.");
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"UPDATE sm_Inbound_StockCII
              SET Status = 'Outward'
              WHERE SerialNumber = @p0
              AND MaterialNumber = @p1",
				new object[] { serialNumber, materialNumber },
				cancellationToken);

			await transaction.CommitAsync(cancellationToken);

			_logger.LogInformation(
				"Return record {Key} deleted for material {MaterialNumber}, serial {SerialNumber}.",
				returnStockCiiKey, materialNumber, serialNumber);
		}

		// ------------------------------------------------------------------ Staging

		public async Task AddStagingAsync(StagingModel data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var userCode = await _context.Database
				.SqlQueryRaw<string>("SELECT Pk_UserCode FROM sm_users WHERE LoginId = @p0", data.UserName)
				.FirstOrDefaultAsync(cancellationToken);

			if (string.IsNullOrEmpty(userCode))
			{
				throw new BadRequestException("Invalid User.");
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"EXEC AddStaging
        @p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7",
				new object?[]
				{
					data.MaterialNumber, data.SerialNumber, data.OrderNumber, data.Type,
					data.DeviceStatus, data.QCDate, data.QCBy, userCode
				}!,
				cancellationToken);

			_logger.LogInformation(
				"Staging record added for material {MaterialNumber}, serial {SerialNumber}.",
				data.MaterialNumber, data.SerialNumber);
		}

		public async Task UpdateStagingAsync(StagingModel data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"EXEC UpsertStaging
        @p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8",
				new object?[]
				{
					data.MaterialNumber, data.SerialNumber, data.OrderNumber, data.Type,
					data.DeviceStatus, data.QCDate, data.QCBy, data.Date, data.UserName
				}!,
				cancellationToken);

			_logger.LogInformation(
				"Staging record updated for material {MaterialNumber}, serial {SerialNumber}.",
				data.MaterialNumber, data.SerialNumber);
		}

		public async Task DeleteStagingAsync(int id, CancellationToken cancellationToken = default)
		{
			await _context.Database.ExecuteSqlRawAsync(
				@"EXEC DeleteStaging @p0",
				new object[] { id },
				cancellationToken);

			_logger.LogInformation("Staging record {StagingId} deleted.", id);
		}

		// ------------------------------------------------------------------ Entity CRUD

		public async Task UpdateAsync(string id, SmOutboundStockCii stock, CancellationToken cancellationToken = default)
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
					throw new NotFoundException($"Outbound CII stock '{id}' was not found.", innerException: exception);
				}

				throw;
			}
		}

		public async Task<SmOutboundStockCii> CreateAsync(SmOutboundStockCii stock, CancellationToken cancellationToken = default)
		{
			if (stock == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			_context.SmOutboundStockCiis.Add(stock);

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateException exception)
			{
				if (await ExistsAsync(stock.DeliveryNumber, cancellationToken))
				{
					throw new ConflictException($"Outbound CII stock '{stock.DeliveryNumber}' already exists.", innerException: exception);
				}

				throw;
			}

			return stock;
		}

		public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var stock = await _context.SmOutboundStockCiis.FindAsync(new object?[] { id }, cancellationToken)
				?? throw NotFoundException.For("Outbound CII stock", id);

			_context.SmOutboundStockCiis.Remove(stock);
			await _context.SaveChangesAsync(cancellationToken);
		}

		// ------------------------------------------------------------------ Helpers

		private async Task GuardSerialNotAlreadyProcessedAsync(string serial, string? materialNumber, CancellationToken cancellationToken)
		{
			var status = await _context.Database
				.SqlQueryRaw<string>(
					"SELECT Status FROM sm_Inbound_StockCII WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
					serial, materialNumber)
				.ToListAsync(cancellationToken);

			if (status.Count > 0 && AlreadyProcessedStatuses.Contains(status[0], StringComparer.OrdinalIgnoreCase))
			{
				throw new BusinessException(StatusCodes.Status400BadRequest, $"Serial {serial} already processed.");
			}
		}

		private Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
			=> _context.SmOutboundStockCiis.AsNoTracking().AnyAsync(e => e.DeliveryNumber == id, cancellationToken);

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
				throw new BadRequestException("Request data is null.");
			}
		}
	}
}
