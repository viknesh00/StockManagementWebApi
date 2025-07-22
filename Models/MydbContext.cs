using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models.NonStockCII;
using StockManagementWebApi.Models.UserManagement;

namespace StockManagementWebApi.Models;

public partial class MydbContext : DbContext
{
    public MydbContext()
    {
    }

    public MydbContext(DbContextOptions<MydbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ReturnStockNonCii> ReturnStockNonCiis { get; set; }
    public virtual DbSet<NonStockReturnCii> NonStockReturnCiis { get; set; }
	public virtual DbSet<ReportCii> ReportCiis { get; set; }
	public virtual DbSet<SmCompany> SmCompanies { get; set; }

    public virtual DbSet<SmInboundStockCii> SmInboundStockCiis { get; set; }
    public virtual DbSet<SmInboundStockCiiDelete> SmInboundStockCiiDeletes { get; set; }

    public virtual DbSet<SmInboundStockNonCii> SmInboundStockNonCiis { get; set; }

    public virtual DbSet<SmOutboundStockCii> SmOutboundStockCiis { get; set; }

    public virtual DbSet<SmOutboundStockNonCii> SmOutboundStockNonCiis { get; set; }
    public virtual DbSet<GetNonStockDeliveredData> GetNonStockDeliveredDatas { get; set; }

    public virtual DbSet<SmReturnStockCii> SmReturnStockCiis { get; set; }

    public virtual DbSet<ReturnStockData> ReturnStockDatas { get; set; }

    public virtual DbSet<SmTenent> SmTenents { get; set; }

    public virtual DbSet<SmUser> SmUsers { get; set; }

    public virtual DbSet<SmUserRole> SmUserRoles { get; set; }
	public virtual DbSet<StockCiiList> StockCiiLists { get; set; }
	public virtual DbSet<OutboundDataList> OutboundDataLists { get; set; }
	public virtual DbSet<AddReturnDataList> AddReturnDataLists { get; set; }
	public virtual DbSet<UpdatedeliveryDataList> UpdatedeliveryDataLists { get; set; }
	public virtual DbSet<UpdateReturnDataList> UpdateReturnDataLists { get; set; }
	public virtual DbSet<NonStockCIIList> NonStockCIILists { get; set; }
	public virtual DbSet<StockInboundCIIList> StockInboundCIILists { get; set; }
	public virtual DbSet<InboundCIIList> InboundCIILists { get; set; }
	public virtual DbSet<GetUsedStock> GetUsedStocks { get; set; }
	public virtual DbSet<DashboardList> DashboardLists { get; set; }
	public virtual DbSet<DashboardDeliveryCount> DashboardDeliveryCounts { get; set; }
	public virtual DbSet<AnalyticsDashboard> AnalyticsDashboards { get; set; }
	public virtual DbSet<NonStockInwardList> NonStockInwardLists { get; set; }
	public virtual DbSet<Dashboardchart> Dashboardcharts { get; set; }
    public virtual DbSet<CompanyList> CompanyLists { get; set; }
	public virtual DbSet<TenetList> TenetLists { get; set; }
	public virtual DbSet<UserList> UserLists { get; set; }
	public virtual DbSet<SerialNumberSearch> SerialNumberSearchs { get; set; }
	public virtual DbSet<SmOutBounddata> SmOutBounddatas { get; set; }
    public virtual DbSet<Log_record> Log_records { get; set; }
	public virtual DbSet<MaterialComparisonResult> MaterialComparisonResults { get; set; }
	

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
		modelBuilder.Entity<MaterialComparisonResult>().HasNoKey();
		modelBuilder.Entity<ReportCii>().HasNoKey();
		modelBuilder.Entity<SmOutBounddata>().HasNoKey();
		modelBuilder.Entity<SerialNumberSearch>().HasNoKey();
		modelBuilder.Entity<UserList>().HasNoKey();
		modelBuilder.Entity<TenetList>().HasNoKey();
		modelBuilder.Entity<CompanyList>().HasNoKey();
        modelBuilder.Entity<Dashboardchart>().HasNoKey();
		modelBuilder.Entity<NonStockInwardList>().HasNoKey();
		modelBuilder.Entity<AnalyticsDashboard>().HasNoKey();
		modelBuilder.Entity<DashboardDeliveryCount>().HasNoKey();
		modelBuilder.Entity<DashboardList>().HasNoKey();
		modelBuilder.Entity<GetUsedStock>().HasNoKey();
		modelBuilder.Entity<InboundCIIList>().HasNoKey();
		modelBuilder.Entity<StockInboundCIIList>().HasNoKey();
        modelBuilder.Entity<ReturnStockData>().HasNoKey();
        modelBuilder.Entity<NonStockCIIList>().HasNoKey();
		modelBuilder.Entity<UpdateReturnDataList>().HasNoKey();
        modelBuilder.Entity<Log_record>().HasNoKey();




        modelBuilder.Entity<UpdatedeliveryDataList>().HasNoKey();
		modelBuilder.Entity<AddReturnDataList>().HasNoKey();
		modelBuilder.Entity<StockCiiList>().HasNoKey();
		modelBuilder.Entity<OutboundDataList>().HasNoKey();
        modelBuilder.Entity<NonStockReturnCii>().HasNoKey();
        modelBuilder.Entity<GetNonStockDeliveredData>().HasNoKey();
        modelBuilder.Entity<SmInboundStockCiiDelete>().HasNoKey();
        modelBuilder.Entity<ReturnStockNonCii>(entity =>
        {
            entity.HasKey(e => e.DeliveryNumber).HasName("PK__ReturnSt__CB28B436CF5829ED");

            entity.ToTable("ReturnStock_NonCII");

            entity.Property(e => e.DeliveryNumber).HasMaxLength(45);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.LocationReturnedFrom).HasMaxLength(45);
            entity.Property(e => e.OrderNumber).HasMaxLength(45);
            entity.Property(e => e.ReturnReason).HasMaxLength(45);
            entity.Property(e => e.ReturnType).HasMaxLength(45);
            entity.Property(e => e.ReturnedBy).HasMaxLength(45);
            entity.Property(e => e.ReturnedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<SmCompany>(entity =>
        {
            entity.HasKey(e => e.PkCompanyCode).HasName("PK__sm_Compa__93A1F736368DDFDB");

            entity.ToTable("sm_Companies");

            entity.Property(e => e.PkCompanyCode)
                .HasMaxLength(10)
                .HasColumnName("Pk_CompanyCode");
            entity.Property(e => e.CompanyLocation).HasMaxLength(45);
            entity.Property(e => e.CompanyName).HasMaxLength(45);
            entity.Property(e => e.CompanyStatus).HasMaxLength(10);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<SmInboundStockCii>(entity =>
        {
            entity.HasKey(e => e.DeliveryNumber).HasName("PK__sm_Inbou__CB28B4360E7718BF");

            entity.ToTable("sm_Inbound_StockCII");

            entity.Property(e => e.DeliveryNumber).HasMaxLength(45);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FkUserCode).HasColumnName("Fk_UserCode");
            entity.Property(e => e.InwardDate).HasColumnType("datetime");
            entity.Property(e => e.MaterialDescription).HasMaxLength(1000);
            entity.Property(e => e.MaterialNumber).HasMaxLength(45);
            entity.Property(e => e.OrderNumber).HasMaxLength(45);
            entity.Property(e => e.RackLocation).HasMaxLength(45);
            entity.Property(e => e.ReceivedBy).HasMaxLength(45);
            entity.Property(e => e.SerialNumber).HasMaxLength(45);
            entity.Property(e => e.SourceLocation).HasMaxLength(45);
            entity.Property(e => e.Status).HasMaxLength(45);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            //entity.HasOne(d => d.FkUserCodeNavigation).WithMany(p => p.SmInboundStockCiis)
            //    .HasForeignKey(d => d.FkUserCode)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("fk_sm_Inbound_StockCII_sm_Users");
        });

        modelBuilder.Entity<SmInboundStockNonCii>(entity =>
        {
            entity.HasKey(e => e.DeliveryNumber).HasName("PK__sm_Inbou__CB28B43640096037");

            entity.ToTable("sm_InboundStock_NonCII");

            entity.Property(e => e.DeliveryNumber).HasMaxLength(45);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FkUserCode).HasColumnName("Fk_UserCode");
            entity.Property(e => e.InwardDate).HasColumnType("datetime");
            entity.Property(e => e.MaterialDescription).HasMaxLength(1000);
            entity.Property(e => e.MaterialNumber).HasMaxLength(45);
            entity.Property(e => e.OrderNumber).HasMaxLength(45);
            entity.Property(e => e.RackLocation).HasMaxLength(45);
            entity.Property(e => e.ReceivedBy).HasMaxLength(45);
            entity.Property(e => e.SourceLocation).HasMaxLength(45);
            entity.Property(e => e.Status).HasMaxLength(45);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.FkUserCodeNavigation).WithMany(p => p.SmInboundStockNonCiis)
                .HasForeignKey(d => d.FkUserCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sm_InboundStock_NonCII_sm_Users");
        });

        modelBuilder.Entity<SmOutboundStockCii>(entity =>
        {
            entity.HasKey(e => e.DeliveryNumber).HasName("PK__sm_Outbo__CB28B436B39FE2BD");

            entity.ToTable("sm_Outbound_StockCII");

            entity.Property(e => e.DeliveryNumber).HasMaxLength(45);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FkInboundStockCiiDeliveryNumber)
                .HasMaxLength(45)
                .HasColumnName("Fk_Inbound_StockCII_DeliveryNumber");
            entity.Property(e => e.MaterialDescription).HasMaxLength(1000);
            entity.Property(e => e.MaterialNumber).HasMaxLength(45);
            entity.Property(e => e.OrderNumber).HasMaxLength(45);
            entity.Property(e => e.OutboundDate).HasColumnType("datetime");
            entity.Property(e => e.SentBy).HasMaxLength(45);
            entity.Property(e => e.SerialNumber).HasMaxLength(45);
            entity.Property(e => e.TargetLocation).HasMaxLength(45);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            //entity.HasOne(d => d.FkInboundStockCiiDeliveryNumberNavigation).WithMany(p => p.SmOutboundStockCiis)
            //    .HasForeignKey(d => d.FkInboundStockCiiDeliveryNumber)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("fk_sm_Outbound_StockCII_sm_Inbound_StockCII1");
        });

        modelBuilder.Entity<SmOutboundStockNonCii>(entity =>
        {
            entity.HasKey(e => e.DeliveryNumber).HasName("PK__sm_Outbo__CB28B4361BE4096D");

            entity.ToTable("sm_OutboundStock_NonCII");

            entity.Property(e => e.DeliveryNumber).HasMaxLength(45);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FkInboundStockNonCiiDeliveryNumber)
                .HasMaxLength(45)
                .HasColumnName("Fk_InboundStock_NonCII_DeliveryNumber");
            entity.Property(e => e.MaterialDescription).HasMaxLength(1000);
            entity.Property(e => e.MaterialNumber).HasMaxLength(45);
            entity.Property(e => e.OrderNumber).HasMaxLength(45);
            entity.Property(e => e.OutboundDate).HasColumnType("datetime");
            entity.Property(e => e.SentBy).HasMaxLength(45);
            entity.Property(e => e.TargetLocation).HasMaxLength(45);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.FkInboundStockNonCiiDeliveryNumberNavigation).WithMany(p => p.SmOutboundStockNonCiis)
                .HasForeignKey(d => d.FkInboundStockNonCiiDeliveryNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sm_OutboundStock_NonCII_sm_InboundStock_NonCII");
        });

        modelBuilder.Entity<SmReturnStockCii>(entity =>
        {
            entity.HasKey(e => e.DeliveryNumber).HasName("PK__sm_Retur__CB28B436B94B35ED");

            entity.ToTable("sm_ReturnStock_CII");

            entity.Property(e => e.DeliveryNumber).HasMaxLength(45);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.LocationReturnedFrom).HasMaxLength(45);
            entity.Property(e => e.OrderNumber).HasMaxLength(45);
            entity.Property(e => e.ReturnType).HasMaxLength(45);
            entity.Property(e => e.ReturnedBy).HasMaxLength(45);
            entity.Property(e => e.ReturnedDate).HasColumnType("datetime");
            entity.Property(e => e.ReturnedReason).HasMaxLength(45);
            entity.Property(e => e.SerialNumber).HasMaxLength(45);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<SmTenent>(entity =>
        {
            entity.HasKey(e => e.PkTenentCode).HasName("PK__sm_Tenen__5BD416B9CC761274");

            entity.ToTable("sm_Tenents");

            entity.Property(e => e.PkTenentCode)
                .HasMaxLength(10)
                .HasColumnName("Pk_TenentCode");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FkCompanyCode)
                .HasMaxLength(10)
                .HasColumnName("Fk_CompanyCode");
            entity.Property(e => e.TenentLocation).HasMaxLength(45);
            entity.Property(e => e.TenentStatus).HasMaxLength(10);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.FkCompanyCodeNavigation).WithMany(p => p.SmTenents)
                .HasForeignKey(d => d.FkCompanyCode)
                .HasConstraintName("fk_sm_Tenents_sm_Companies");
        });

        modelBuilder.Entity<SmUser>(entity =>
        {
            entity.HasKey(e => e.PkUserCode).HasName("PK__sm_Users__942B31AD37B0AA57");

            entity.ToTable("sm_Users");

            entity.Property(e => e.PkUserCode)
                .ValueGeneratedNever()
                .HasColumnName("Pk_UserCode");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(20);
            entity.Property(e => e.FkRoleCode)
                .HasMaxLength(12)
                .HasColumnName("Fk_RoleCode");
            entity.Property(e => e.FkTenentCode)
                .HasMaxLength(10)
                .HasColumnName("Fk_TenentCode");
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(20);
            entity.Property(e => e.LoginId).HasMaxLength(45);
            entity.Property(e => e.Password).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.UserStatus).HasMaxLength(10);

            entity.HasOne(d => d.FkRoleCodeNavigation).WithMany(p => p.SmUsers)
                .HasForeignKey(d => d.FkRoleCode)
                .HasConstraintName("fk_sm_Users_sm_UserRoles");

            entity.HasOne(d => d.FkTenentCodeNavigation).WithMany(p => p.SmUsers)
                .HasForeignKey(d => d.FkTenentCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sm_Users_sm_Tenents");
        });

        modelBuilder.Entity<SmUserRole>(entity =>
        {
            entity.HasKey(e => e.PkRoleCode).HasName("PK__sm_UserR__CAF0EEB915B6A031");

            entity.ToTable("sm_UserRoles");

            entity.Property(e => e.PkRoleCode)
                .HasMaxLength(12)
                .HasColumnName("Pk_RoleCode");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.RoleName).HasMaxLength(45);
            entity.Property(e => e.RoleStatus).HasMaxLength(10);
            entity.Property(e => e.UpdatedBy).HasMaxLength(45);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
