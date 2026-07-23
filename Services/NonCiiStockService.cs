using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Common.Files;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.NonStockCII;
using StockManagementWebApi.Models.Notifications;
using StockManagementWebApi.Models.Responses;

namespace StockManagementWebApi.Services
{
	public interface INonCiiStockService
	{
		Task<IReadOnlyList<NonStockCIIList>> GetStockListAsync(string userName, CancellationToken cancellationToken = default);

		Task BulkUpdateSerialsAsync(BulkUpdateRequest request, CancellationToken cancellationToken = default);

		Task BulkImportAsync(AddNonCIIStockInward data, CancellationToken cancellationToken = default);

		Task AddMaterialAsync(AddMaterial data, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<NonStockInwardList>> GetInwardListAsync(string materialNumber, string userName, CancellationToken cancellationToken = default);

		Task AddInboundAsync(AddNonStockInward data, CancellationToken cancellationToken = default);

		Task DeleteInboundAsync(string materialNumber, string deliveryNumber, string inboundStockNonCiiKey, string userName, CancellationToken cancellationToken = default);

		Task UpdateInboundAsync(UpdateNonStockInward data, CancellationToken cancellationToken = default);

		Task AddOutboundAsync(AddOutBoundNonStockCII data, CancellationToken cancellationToken = default);

		Task BulkAddOutboundAsync(List<BulkAddOutboundDataNonStockCii> dataList, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<GetNonStockDeliveredData>> GetDeliveredListAsync(string materialNumber, string UserName , CancellationToken cancellationToken = default);

		Task DeleteDeliveredAsync(string materialNumber, string deliveryNumber, string outboundStockNonCiiKey, string userName, CancellationToken cancellationToken = default);

		Task UpdateDeliveredAsync(UpdateNonStockDeliverData data, CancellationToken cancellationToken = default);

		Task AddReturnAsync(AddNonStockReturnData data, CancellationToken cancellationToken = default);

		Task UpdateReturnAsync(UpdateNonStockRetundata data, CancellationToken cancellationToken = default);

		Task UpdateReturnTypeAsync(string materialNumber, string orderNumber, CancellationToken cancellationToken = default);

		Task DeleteReturnAsync(string materialNumber, string orderNumber, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<NonStockReturnCii>> GetReturnListAsync(string materialNumber, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<GetUsedStock>> GetUsedStockListAsync(string materialNumber, CancellationToken cancellationToken = default);

		Task AddUsedStockAsync(AddUsedStock data, CancellationToken cancellationToken = default);

		Task UpdateUsedStockAsync(UpdateUsedStock data, CancellationToken cancellationToken = default);

		Task DeleteUsedStockAsync(string materialNumber, string orderNumber, CancellationToken cancellationToken = default);

		Task<DashboardSummary> GetDashboardAsync(string userName, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<AnalyticsDashboard>> GetCiiAnalyticsAsync(string materialNumber, string UserName, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<AnalyticsDashboard>> GetNonCiiAnalyticsAsync(string materialNumber, string UserName , CancellationToken cancellationToken = default);

		Task<DashboardChartSummary> GetDashboardChartsAsync(string userName, CancellationToken cancellationToken = default);

		Task<SmInboundStockNonCii> GetByIdAsync(string id, CancellationToken cancellationToken = default);

		Task UpdateAsync(string id, SmInboundStockNonCii stock, CancellationToken cancellationToken = default);

		Task<SmInboundStockNonCii> CreateAsync(SmInboundStockNonCii stock, CancellationToken cancellationToken = default);

		Task DeleteAsync(string id, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Non-CII (quantity-tracked) stock: inbound, outbound, returns, used stock, dashboards and
	/// the Excel bulk import.
	/// </summary>
	public class NonCiiStockService : INonCiiStockService
	{
		private readonly MydbContext _context;
		private readonly IUploadedFileStore _fileStore;
		private readonly INotificationPublisher _notifications;
		private readonly ILogger<NonCiiStockService> _logger;

		public NonCiiStockService(
			MydbContext context,
			IUploadedFileStore fileStore,
			INotificationPublisher notifications,
			ILogger<NonCiiStockService> logger)
		{
			_context = context;
			_fileStore = fileStore;
			_notifications = notifications;
			_logger = logger;
		}

		// ------------------------------------------------------------------ Listings

		public async Task<IReadOnlyList<NonStockCIIList>> GetStockListAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return await _context.NonStockCIILists
				.FromSqlRaw(@"exec Non_StockCIIList @p0", userName)
				.ToListAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<NonStockInwardList>> GetInwardListAsync(string materialNumber, string userName, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(userName, nameof(userName));

			return await _context.NonStockInwardLists
				.FromSqlRaw(@"exec sp_getInboundStock_NonCII @p0,@p1", materialNumber, userName)
				.ToListAsync(cancellationToken);
		}

		// ------------------------------------------------------------------ Bulk serial update

		public async Task BulkUpdateSerialsAsync(BulkUpdateRequest request, CancellationToken cancellationToken = default)
		{
			if (request == null || string.IsNullOrEmpty(request.MaterialNumber) || request.SerialNumbers == null || !request.SerialNumbers.Any())
			{
				throw new ValidationException("Invalid request");
			}

			// The serial numbers are bound as individual parameters rather than concatenated into
			// the statement, so a serial number containing a quote can no longer alter the query.
			var serialParameters = request.SerialNumbers
				.Select((serial, index) => new SqlParameter($"@serial{index}", (object?)serial ?? DBNull.Value))
				.ToList();

			var inClause = string.Join(",", serialParameters.Select(p => p.ParameterName));

			var query = @"
        UPDATE sm_Inbound_StockCII
        SET
            RackLocation = CASE
                              WHEN @RackLocation IS NOT NULL AND @RackLocation <> ''
                              THEN @RackLocation
                              ELSE RackLocation
                           END,
            Status = CASE
                        WHEN @Status IS NOT NULL AND @Status <> ''
                        THEN @Status
                        ELSE Status
                     END
        WHERE MaterialNumber = @MaterialNumber
        AND SerialNumber IN (" + inClause + ")";

			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@MaterialNumber", request.MaterialNumber),
				new SqlParameter("@RackLocation", (object?)request.RackLocation ?? DBNull.Value),
				new SqlParameter("@Status", (object?)request.Status ?? DBNull.Value)
			};
			parameters.AddRange(serialParameters);

			await _context.Database.ExecuteSqlRawAsync(query, parameters, cancellationToken);

			_logger.LogInformation(
				"Bulk updated {SerialCount} serial number(s) of material {MaterialNumber}.",
				request.SerialNumbers.Count, request.MaterialNumber);
		}

		// ------------------------------------------------------------------ Bulk import

		public async Task BulkImportAsync(AddNonCIIStockInward data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var filePath = await _fileStore.SaveAsync(data.file, cancellationToken);

			try
			{
				var rows = ReadImportRows(filePath);

				var tenantCode = await _context.Database
					.SqlQuery<string>($"SELECT Fk_TenentCode AS Value FROM [dbo].[sm_Users] WHERE LoginId = {data.UserName}")
					.FirstOrDefaultAsync(cancellationToken);

				foreach (var row in rows)
				{
					var materialNumber = row.MaterialNumber;
					var materialDescription = row.MaterialDescription;

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
							@"EXEC AddNonStockCII_MaterialNumber @p0, @p1, @p2",
							new object?[] { data.UserName, materialNumber, materialDescription }!,
							cancellationToken);
					}

					await _context.Database.ExecuteSqlRawAsync(
						@"exec Sp_AddInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12",
						new object?[]
						{
							data.DeliveryNumber, data.OrderNumber, materialNumber, materialDescription,
							data.InwardDate, data.InwardFrom, data.ReceivedBy, data.RackLocation,
							row.Quantity, data.UserName, data.PoNumber, data.Location, row.Status
						}!,
						cancellationToken);
				}

				_logger.LogInformation(
					"Non-CII bulk import completed for {UserName}: {RowCount} row(s).",
					data.UserName, rows.Count);

				await _notifications.PublishAsync(
					data.UserName,
					NotificationTypes.StockInward,
					NotificationSeverities.Success,
					"Bulk non-CII stock inwarded",
					$"{rows.Count} non-CII material line(s) were inwarded against delivery {data.DeliveryNumber}.",
					referenceType: "NonCiiInward",
					referenceId: data.DeliveryNumber,
					cancellationToken: cancellationToken);
			}
			catch (SqlException exception) when (exception.IsApplicationRaised())
			{
				// Messages raised by the import stored procedures are written for end users.
				throw new BusinessException(StatusCodes.Status400BadRequest, exception.Message, innerException: exception);
			}
			finally
			{
				_fileStore.TryDelete(filePath);
			}
		}

		// ------------------------------------------------------------------ Material master

		public async Task AddMaterialAsync(AddMaterial data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			try
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"exec AddNonStockCII_MaterialNumber @p0, @p1, @p2",
					new object?[] { data.userName, data.MaterialNumber, data.MaterialDescription }!,
					cancellationToken);
			}
			catch (SqlException exception) when (exception.Number == SqlErrorCodes.UniqueConstraintViolation)
			{
				throw new BusinessException(
					StatusCodes.Status400BadRequest,
					"Duplicate entry: The material number already exists.",
					innerException: exception);
			}

			_logger.LogInformation("Non-CII material {MaterialNumber} created by {UserName}.", data.MaterialNumber, data.userName);
		}

		// ------------------------------------------------------------------ Inbound

		public async Task AddInboundAsync(AddNonStockInward data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_AddInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9, @p10, @p11, @p12",
				new object?[]
				{
					data.DeliveryNumber, data.OrderNumber, data.MaterialNumber, data.MaterialDescription,
					data.Inwarddate, data.InwardFrom, data.ReceivedBy, data.RacKLocation,
					data.QuantityReceived, data.UserName, data.PoNumber, data.Location, data.Status
				}!,
				cancellationToken);

			_logger.LogInformation("Non-CII inbound recorded for material {MaterialNumber}.", data.MaterialNumber);

			await _notifications.PublishAsync(
				data.UserName,
				NotificationTypes.StockInward,
				NotificationSeverities.Success,
				"Non-CII stock inwarded",
				$"{data.QuantityReceived} unit(s) of material {data.MaterialNumber} were inwarded against delivery {data.DeliveryNumber}.",
				materialNumber: data.MaterialNumber,
				referenceType: "NonCiiInward",
				referenceId: data.DeliveryNumber,
				cancellationToken: cancellationToken);
		}

		public async Task DeleteInboundAsync(string materialNumber, string deliveryNumber, string inboundStockNonCiiKey, string userName, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(deliveryNumber, nameof(deliveryNumber));

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_DeleteNonStockCII @p0, @p1,@p2,@p3",
				new object?[] { userName, materialNumber, deliveryNumber, inboundStockNonCiiKey }!,
				cancellationToken);

			_logger.LogInformation(
				"Non-CII inbound record {Key} deleted for material {MaterialNumber}.",
				inboundStockNonCiiKey, materialNumber);
		}

		public async Task UpdateInboundAsync(UpdateNonStockInward data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_UpdateInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14",
				new object?[]
				{
					data.UserName, data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.ExistDeliveryNumber, data.ExistOrderNumber, data.Inwarddate, data.InwardFrom,
					data.ReceivedBy, data.RacKLocation, data.QuantityReceived, data.InboundStockNonCIIKey,
					data.PoNumber, data.Status, data.Location
				}!,
				cancellationToken);

			_logger.LogInformation("Non-CII inbound updated for material {MaterialNumber}.", data.MaterialNumber);
		}

		// ------------------------------------------------------------------ Outbound

		public async Task AddOutboundAsync(AddOutBoundNonStockCII data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var stockRows = await _context.SmOutBounddatas
				.FromSqlRaw(
					@"SELECT * FROM sm_InboundStock_NonCII
			              WHERE materialnumber = @p0 AND IsActive = 1  ",
					data.MaterialNumber)
				.ToListAsync(cancellationToken);

			if (stockRows.Count == 0)
			{
				throw new NotFoundException("No matching inbound stock record found.");
			}

			var totalInboundQuantity = stockRows.Sum(c => c.DeliveredQuantity);

			if (totalInboundQuantity < data.DeliveredQuantity)
			{
				throw new BusinessException("Delivered quantity exceeds in-stock quantity. Please adjust the delivered quantity.");
			}

			// Deduct the requested quantity from the available inbound rows, oldest row first.
			var remainingQuantityToWithdraw = data.DeliveredQuantity ?? 0;

			foreach (var stockItem in stockRows)
			{
				if (stockItem.DeliveredQuantity <= remainingQuantityToWithdraw)
				{
					remainingQuantityToWithdraw -= stockItem.DeliveredQuantity;
					stockItem.DeliveredQuantity = 0;
				}
				else
				{
					stockItem.DeliveredQuantity -= remainingQuantityToWithdraw;
					remainingQuantityToWithdraw = 0;
					break;
				}
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"EXEC Sp_AddOutboundStock_NonCII
			              @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9,@p10",
				new object?[]
				{
					data.UserName, data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.MaterialDescription, data.OutboundDate, data.ReceiverName,
					data.TargetLocation, data.DeliveredQuantity, data.SentBy, data.DeliveryNumber_inbound
				}!,
				cancellationToken);

			foreach (var stockItem in stockRows)
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"UPDATE sm_InboundStock_NonCII
			              SET DeliveredQuantity = @p0
			              WHERE materialnumber = @p1 AND deliverynumber = @p2 and InboundStockNonCIIKey= @p3",
					new object?[]
					{
						stockItem.DeliveredQuantity, data.MaterialNumber,
						stockItem.DeliveryNumber, stockItem.InboundStockNonCIIKey
					}!,
					cancellationToken);
			}

			_logger.LogInformation(
				"Non-CII outbound recorded for material {MaterialNumber}, quantity {Quantity}.",
				data.MaterialNumber, data.DeliveredQuantity);

			await _notifications.PublishAsync(
				data.UserName,
				NotificationTypes.StockOutward,
				NotificationSeverities.Info,
				"Non-CII stock delivered",
				$"{data.DeliveredQuantity} unit(s) of material {data.MaterialNumber} were delivered to {data.TargetLocation ?? "the target location"}.",
				materialNumber: data.MaterialNumber,
				referenceType: "NonCiiOutward",
				referenceId: data.DeliveryNumber,
				cancellationToken: cancellationToken);
		}

		public async Task BulkAddOutboundAsync(List<BulkAddOutboundDataNonStockCii> dataList, CancellationToken cancellationToken = default)
		{
			if (dataList == null || dataList.Count == 0)
			{
				throw new ValidationException("No data received.");
			}

			await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

			foreach (var data in dataList)
			{
				var stockRows = await _context.SmOutBounddatas
					.FromSqlRaw(@"
                    SELECT *
                    FROM sm_InboundStock_NonCII
                    WHERE MaterialNumber = @p0
                      AND IsActive = 1",
						data.MaterialNumber)
					.ToListAsync(cancellationToken);

				if (stockRows.Count == 0)
				{
					throw new BusinessException($"No inbound stock found for Material Number : {data.MaterialNumber}");
				}

				var totalInboundQuantity = stockRows.Sum(x => x.DeliveredQuantity);

				if (totalInboundQuantity < data.DeliveredQuantity)
				{
					throw new BusinessException($"Insufficient stock for Material Number : {data.MaterialNumber}");
				}

				var remainingQuantity = data.DeliveredQuantity ?? 0;

				foreach (var stock in stockRows)
				{
					if (remainingQuantity == 0)
					{
						break;
					}

					if (stock.DeliveredQuantity <= remainingQuantity)
					{
						remainingQuantity -= stock.DeliveredQuantity;
						stock.DeliveredQuantity = 0;
					}
					else
					{
						stock.DeliveredQuantity -= remainingQuantity;
						remainingQuantity = 0;
					}
				}

				await _context.Database.ExecuteSqlRawAsync(
					@"EXEC Sp_BulkAddOutboundStock_NonCII
                    @p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12",
					new object?[]
					{
						data.UserName, data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
						data.MaterialDescription, data.OutboundDate, data.ReceiverName,
						data.TargetLocation, data.DeliveredQuantity, data.SentBy,
						data.DeliveryNumber_inbound, data.Status, data.SubStatus
					}!,
					cancellationToken);

				foreach (var stock in stockRows)
				{
					await _context.Database.ExecuteSqlRawAsync(
						@"UPDATE sm_InboundStock_NonCII
                      SET DeliveredQuantity = @p0
                      WHERE InboundStockNonCIIKey = @p1",
						new object[] { stock.DeliveredQuantity, stock.InboundStockNonCIIKey },
						cancellationToken);
				}
			}

			await transaction.CommitAsync(cancellationToken);

			_logger.LogInformation("Non-CII bulk outward completed for {RowCount} row(s).", dataList.Count);

			// Published after the commit, so a rolled-back batch leaves no notification behind.
			await _notifications.PublishAsync(
				dataList[0].UserName,
				NotificationTypes.StockOutward,
				NotificationSeverities.Info,
				"Bulk non-CII stock delivered",
				$"{dataList.Count} non-CII material line(s) were delivered.",
				referenceType: "NonCiiOutward",
				referenceId: dataList[0].DeliveryNumber,
				cancellationToken: cancellationToken);
		}

        public async Task<IReadOnlyList<GetNonStockDeliveredData>> GetDeliveredListAsync(string materialNumber, string UserName, CancellationToken cancellationToken = default)
        {
            Require(materialNumber, nameof(materialNumber));
            Require(UserName, nameof(UserName));

            return await _context.GetNonStockDeliveredDatas
                .FromSqlRaw("exec dbo.sp_getDeliveredStock_NonCII @p0, @p1", materialNumber, UserName)
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteDeliveredAsync(string materialNumber, string deliveryNumber, string outboundStockNonCiiKey, string userName, CancellationToken cancellationToken = default)
		{
			var errors = new List<string>();
			if (string.IsNullOrWhiteSpace(materialNumber))
			{
				errors.Add("MaterialNumber is required.");
			}

			if (string.IsNullOrWhiteSpace(deliveryNumber))
			{
				errors.Add("DeliveryNumber is required.");
			}

			if (errors.Count > 0)
			{
				throw new ValidationException(errors, "MaterialNumber and DeliveryNumber are required.");
			}

			var outboundStock = await _context.GetNonStockDeliveredDatas
				.FromSqlRaw(
					"SELECT * FROM sm_OutboundStock_NonCII WHERE materialnumber = @MaterialNumber AND deliverynumber = @DeliveryNumber AND OutboundStockNonCIIKey = @OutboundStockNonCIIKey AND IsActive <> 0",
					new SqlParameter("@MaterialNumber", materialNumber),
					new SqlParameter("@DeliveryNumber", deliveryNumber),
					new SqlParameter("@OutboundStockNonCIIKey", outboundStockNonCiiKey))
				.FirstOrDefaultAsync(cancellationToken);

			if (outboundStock == null)
			{
				throw new NotFoundException("No matching outbound stock record found.");
			}

			var deliveredQuantityToReturn = outboundStock.DeliveredQuantity;

			var inboundStocks = await _context.SmOutBounddatas
				.FromSqlRaw(
					@"SELECT * FROM sm_InboundStock_NonCII
                  WHERE materialnumber = @p0 and IsActive = 1 ",
					new SqlParameter("@p0", materialNumber))
				.OrderBy(i => i.InboundStockNonCIIKey)
				.ToListAsync(cancellationToken);

			if (inboundStocks.Count == 0)
			{
				throw new NotFoundException("No matching inbound stock records found.");
			}

			// Return the delivered quantity to the first inbound row.
			var remainingToAdd = deliveredQuantityToReturn;
			foreach (var stock in inboundStocks)
			{
				stock.DeliveredQuantity += remainingToAdd;
				break;
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"EXEC Sp_DeleteNonStockDeliverCII @p0, @p1, @p2,@p3",
				new object[]
				{
					new SqlParameter("@p0", userName),
					new SqlParameter("@p1", materialNumber),
					new SqlParameter("@p2", deliveryNumber),
					new SqlParameter("@p3", outboundStockNonCiiKey)
				},
				cancellationToken);

			foreach (var stock in inboundStocks)
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"UPDATE sm_InboundStock_NonCII
                  SET DeliveredQuantity = @p0
                  WHERE materialnumber = @p1 AND InboundStockNonCIIKey = @p2",
					new object[]
					{
						new SqlParameter("@p0", stock.DeliveredQuantity),
						new SqlParameter("@p1", materialNumber),
						new SqlParameter("@p2", stock.InboundStockNonCIIKey)
					},
					cancellationToken);
			}

			_logger.LogInformation(
				"Non-CII delivery {Key} deleted for material {MaterialNumber}; {Quantity} returned to stock.",
				outboundStockNonCiiKey, materialNumber, deliveredQuantityToReturn);
		}

		public async Task UpdateDeliveredAsync(UpdateNonStockDeliverData data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var outboundStock = await _context.GetNonStockDeliveredDatas
				.FromSqlRaw(@"SELECT * FROM sm_OutboundStock_NonCII
                         WHERE materialnumber = @p0 AND deliverynumber = @p1 and OutboundStockNonCIIKey= @p2 and IsActive = 1 ",
					data.MaterialNumber, data.DeliveryNumber, data.OutboundStockNonCIIKey)
				.FirstOrDefaultAsync(cancellationToken);

			if (outboundStock == null)
			{
				throw new NotFoundException("No matching outbound stock record found.");
			}

			var existingOutboundQuantity = data.ExistDeliveredQuantity ?? 0;
			var quantityDifference = (data.DeliveredQuantity ?? 0) - existingOutboundQuantity;

			var inboundStocks = await _context.SmOutBounddatas
				.FromSqlRaw(@"SELECT * FROM sm_InboundStock_NonCII
                         WHERE materialnumber = @p0 AND IsActive = 1",
					data.MaterialNumber)
				.OrderBy(i => i.InboundStockNonCIIKey)
				.ToListAsync(cancellationToken);

			if (inboundStocks.Count == 0)
			{
				throw new NotFoundException("No matching inbound stock record found.");
			}

			if (quantityDifference > 0)
			{
				// The delivery grew: take the difference out of inbound stock.
				var remainingToDeduct = quantityDifference;

				foreach (var stock in inboundStocks)
				{
					if (stock.DeliveredQuantity >= remainingToDeduct)
					{
						stock.DeliveredQuantity -= remainingToDeduct;
						remainingToDeduct = 0;
						break;
					}

					remainingToDeduct -= stock.DeliveredQuantity;
					stock.DeliveredQuantity = 0;
				}

				if (remainingToDeduct > 0)
				{
					throw new BusinessException(
						StatusCodes.Status400BadRequest,
						"Not enough available stock to fulfill the increase in outbound quantity.");
				}
			}
			else if (quantityDifference < 0)
			{
				// The delivery shrank: put the difference back on the first inbound row.
				var remainingToAdd = -quantityDifference;

				foreach (var stock in inboundStocks)
				{
					stock.DeliveredQuantity += remainingToAdd;
					break;
				}
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"EXEC Sp_UpdateOutboundStock_NonCII
              @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9,@p10,@p11",
				new object?[]
				{
					data.UserName, data.DeliveryNumber, data.OrderNumber, data.ExistDeliveryNumber,
					data.ExistOrderNumber, data.MaterialNumber, data.OutboundDate, data.ReceiverName,
					data.TargetLocation, data.DeliveredQuantity, data.SentBy, data.OutboundStockNonCIIKey
				}!,
				cancellationToken);

			foreach (var stock in inboundStocks)
			{
				await _context.Database.ExecuteSqlRawAsync(
					@"UPDATE sm_InboundStock_NonCII
                  SET DeliveredQuantity = @p0
                  WHERE materialnumber = @p1 AND InboundStockNonCIIKey = @p2",
					new object?[] { stock.DeliveredQuantity, data.MaterialNumber, stock.InboundStockNonCIIKey }!,
					cancellationToken);
			}

			_logger.LogInformation(
				"Non-CII delivery {Key} updated for material {MaterialNumber}.",
				data.OutboundStockNonCIIKey, data.MaterialNumber);
		}

		// ------------------------------------------------------------------ Returns

		public async Task AddReturnAsync(AddNonStockReturnData data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec sp_AddReturnNonStockData @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8",
				new object?[]
				{
					data.OrderNumber, data.MaterialNumber, data.ReturnLocation, data.Returndate,
					data.ReturnQuantity, data.ReceivedBy, data.RackLocation, data.ReturnType, data.Reason
				}!,
				cancellationToken);

			_logger.LogInformation("Non-CII return recorded for material {MaterialNumber}.", data.MaterialNumber);

			var isFaulty = data.ReturnType is not null &&
				new[] { "Damaged", "BreakFix", "Defective" }.Contains(data.ReturnType, StringComparer.OrdinalIgnoreCase);

			await _notifications.PublishAsync(
				null, // this payload carries no user; the publisher uses the token identity
				NotificationTypes.StockReturn,
				isFaulty ? NotificationSeverities.Warning : NotificationSeverities.Info,
				isFaulty ? $"Non-CII stock returned as {data.ReturnType}" : "Non-CII stock returned",
				$"{data.ReturnQuantity} unit(s) of material {data.MaterialNumber} were returned from {data.ReturnLocation ?? "an unspecified location"}.",
				materialNumber: data.MaterialNumber,
				referenceType: "NonCiiReturn",
				referenceId: data.OrderNumber,
				cancellationToken: cancellationToken);
		}

		public async Task UpdateReturnAsync(UpdateNonStockRetundata data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec sp_UpdateReturnNonStockData @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9",
				new object?[]
				{
					data.OrderNumber, data.ExistOrderNumber, data.MaterialNumber, data.ReturnLocation,
					data.Returndate, data.ReturnQuantity, data.ReceivedBy, data.RackLocation,
					data.ReturnType, data.Reason
				}!,
				cancellationToken);

			_logger.LogInformation("Non-CII return updated for material {MaterialNumber}.", data.MaterialNumber);
		}

		public async Task UpdateReturnTypeAsync(string materialNumber, string orderNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(orderNumber, nameof(orderNumber));

			await _context.Database.ExecuteSqlRawAsync(
				@"exec sp_UpdateNonStockReturnType @p0, @p1",
				new object[] { materialNumber, orderNumber },
				cancellationToken);

			_logger.LogInformation(
				"Non-CII return type updated for material {MaterialNumber}, order {OrderNumber}.",
				materialNumber, orderNumber);
		}

		public async Task DeleteReturnAsync(string materialNumber, string orderNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(orderNumber, nameof(orderNumber));

			await _context.Database.ExecuteSqlRawAsync(
				@"exec sp_DeleteNonStockReturndata @p0, @p1",
				new object[] { materialNumber, orderNumber },
				cancellationToken);

			_logger.LogInformation(
				"Non-CII return deleted for material {MaterialNumber}, order {OrderNumber}.",
				materialNumber, orderNumber);
		}

		public async Task<IReadOnlyList<NonStockReturnCii>> GetReturnListAsync(string materialNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));

			// Materialised here rather than handing an IQueryable to the serializer, so that a
			// database failure is caught by the exception middleware instead of surfacing
			// mid-response.
			return await _context.NonStockReturnCiis
				.FromSqlRaw(@"exec sp_GateNonStockReturnData @p0", materialNumber)
				.ToListAsync(cancellationToken);
		}

		// ------------------------------------------------------------------ Used stock

		public async Task<IReadOnlyList<GetUsedStock>> GetUsedStockListAsync(string materialNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));

			return await _context.GetUsedStocks
				.FromSqlRaw(@"exec Sp_GetUsedStock_NonCII @p0", materialNumber)
				.ToListAsync(cancellationToken);
		}

		public async Task AddUsedStockAsync(AddUsedStock data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_AddUsedStock_NonCII @p0,@p1,@p2,@p3,@p4",
				new object?[]
				{
					data.OrderNumber, data.MaterialNumber, data.ReturnDate,
					data.ReturnLocation, data.ItemQuantity
				}!,
				cancellationToken);

			_logger.LogInformation("Used stock recorded for material {MaterialNumber}.", data.MaterialNumber);
		}

		public async Task UpdateUsedStockAsync(UpdateUsedStock data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_UpdateUsedStock_NonCII @p0,@p1,@p2,@p3,@p4,@p5",
				new object?[]
				{
					data.OrderNumber, data.ExistOrderNumber, data.MaterialNumber,
					data.ReturnDate, data.ReturnLocation, data.ItemQuantity
				}!,
				cancellationToken);

			_logger.LogInformation("Used stock updated for material {MaterialNumber}.", data.MaterialNumber);
		}

		public async Task DeleteUsedStockAsync(string materialNumber, string orderNumber, CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
			Require(orderNumber, nameof(orderNumber));

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_DeleteUsedStock_NonCII @p0,@p1",
				new object[] { orderNumber, materialNumber },
				cancellationToken);

			_logger.LogInformation(
				"Used stock deleted for material {MaterialNumber}, order {OrderNumber}.",
				materialNumber, orderNumber);
		}

		// ------------------------------------------------------------------ Dashboards

		public async Task<DashboardSummary> GetDashboardAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return new DashboardSummary
			{
				CIICounts = await _context.DashboardLists
					.FromSqlRaw(@"exec dashboard_cii_stock @p0", userName)
					.ToListAsync(cancellationToken),
				NonCIICounts = await _context.DashboardLists
					.FromSqlRaw(@"exec dashboard_Non_StockCIIList @p0", userName)
					.ToListAsync(cancellationToken),
				DeliveryReturnCounts = await _context.DashboardDeliveryCounts
					.FromSqlRaw(@"exec dashboard_delivery_return_count @p0", userName)
					.ToListAsync(cancellationToken)
			};
		}

		public async Task<IReadOnlyList<AnalyticsDashboard>> GetCiiAnalyticsAsync(string materialNumber, string UserName, CancellationToken cancellationToken = default)
		{
            Require(materialNumber, nameof(materialNumber));
            Require(UserName, nameof(UserName));

            return await _context.AnalyticsDashboards
				.FromSqlRaw(@"exec AnalyticsCount_CII @p0,@p1", materialNumber, UserName)
				.ToListAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<AnalyticsDashboard>> GetNonCiiAnalyticsAsync(string materialNumber, string UserName , CancellationToken cancellationToken = default)
		{
			Require(materialNumber, nameof(materialNumber));
            Require(UserName, nameof(UserName));

            return await _context.AnalyticsDashboards
				.FromSqlRaw(@"exec AnalyticsCount_NonCII @p0,@p1", materialNumber, UserName)
				.ToListAsync(cancellationToken);
		}

		public async Task<DashboardChartSummary> GetDashboardChartsAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return new DashboardChartSummary
			{
				CIIInwardCounts = await _context.Dashboardcharts
					.FromSqlRaw(@" exec sp_CIIInwardCount @p0", userName)
					.ToListAsync(cancellationToken),
				NonCIIInwardCounts = await _context.Dashboardcharts
					.FromSqlRaw(@"exec sp_NonCIIInwardCount @p0", userName)
					.ToListAsync(cancellationToken),
				CIIDeliveryCounts = await _context.Dashboardcharts
					.FromSqlRaw(@" exec sp_CIIDeliveryCount @p0", userName)
					.ToListAsync(cancellationToken),
				NonCIIDeliveryCounts = await _context.Dashboardcharts
					.FromSqlRaw(@"exec sp_NonCIIDeliveryCount @p0", userName)
					.ToListAsync(cancellationToken)
			};
		}

		// ------------------------------------------------------------------ Entity CRUD

		public async Task<SmInboundStockNonCii> GetByIdAsync(string id, CancellationToken cancellationToken = default)
		{
			var stock = await _context.SmInboundStockNonCiis.FindAsync(new object?[] { id }, cancellationToken);

			return stock ?? throw NotFoundException.For("Inbound non-CII stock", id);
		}

		public async Task UpdateAsync(string id, SmInboundStockNonCii stock, CancellationToken cancellationToken = default)
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
					throw new NotFoundException($"Inbound non-CII stock '{id}' was not found.", innerException: exception);
				}

				throw;
			}
		}

		public async Task<SmInboundStockNonCii> CreateAsync(SmInboundStockNonCii stock, CancellationToken cancellationToken = default)
		{
			if (stock == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			_context.SmInboundStockNonCiis.Add(stock);

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateException exception)
			{
				if (await ExistsAsync(stock.DeliveryNumber, cancellationToken))
				{
					throw new ConflictException($"Inbound non-CII stock '{stock.DeliveryNumber}' already exists.", innerException: exception);
				}

				throw;
			}

			return stock;
		}

		public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var stock = await _context.SmInboundStockNonCiis.FindAsync(new object?[] { id }, cancellationToken)
				?? throw NotFoundException.For("Inbound non-CII stock", id);

			_context.SmInboundStockNonCiis.Remove(stock);
			await _context.SaveChangesAsync(cancellationToken);
		}

		// ------------------------------------------------------------------ Helpers

		private sealed record ImportRow(string MaterialNumber, string MaterialDescription, int Quantity, string Status);

		private static List<ImportRow> ReadImportRows(string filePath)
		{
			var rows = new List<ImportRow>();

			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			using var package = new ExcelPackage(new FileInfo(filePath));
			var worksheet = package.Workbook.Worksheets[0];
			if (worksheet.Dimension == null)
			{
				throw new BadRequestException("Excel file is empty.");
			}

			var rowCount = worksheet.Dimension.Rows;
			for (var row = 2; row <= rowCount; row++)
			{
				var materialNumber = worksheet.Cells[row, 1].Text.Trim();
				if (string.IsNullOrWhiteSpace(materialNumber))
				{
					continue;
				}

				int.TryParse(worksheet.Cells[row, 3].Text, out var quantity);

				rows.Add(new ImportRow(
					materialNumber,
					worksheet.Cells[row, 2].Text.Trim(),
					quantity,
					worksheet.Cells[row, 4].Text.Trim()));
			}

			return rows;
		}

		private Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
			=> _context.SmInboundStockNonCiis.AsNoTracking().AnyAsync(e => e.DeliveryNumber == id, cancellationToken);

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
