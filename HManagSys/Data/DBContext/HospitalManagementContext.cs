using System;
using System.Collections.Generic;
using HManagSys.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Data.DBContext;

public partial class HospitalManagementContext : DbContext
{
    public HospitalManagementContext()
    {
    }

    public HospitalManagementContext(DbContextOptions<HospitalManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApplicationLog> ApplicationLogs { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CareEpisode> CareEpisodes { get; set; }

    public virtual DbSet<CareService> CareServices { get; set; }

    public virtual DbSet<CareServiceProduct> CareServiceProducts { get; set; }

    public virtual DbSet<CareType> CareTypes { get; set; }

    public virtual DbSet<CashHandover> CashHandovers { get; set; }

    public virtual DbSet<Diagnosis> Diagnoses { get; set; }

    public virtual DbSet<Examination> Examinations { get; set; }

    public virtual DbSet<ExaminationResult> ExaminationResults { get; set; }

    public virtual DbSet<ExaminationType> ExaminationTypes { get; set; }

    public virtual DbSet<Financier> Financiers { get; set; }

    public virtual DbSet<HospitalCenter> HospitalCenters { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<PrescriptionItem> PrescriptionItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<RptActiveSession> RptActiveSessions { get; set; }

    public virtual DbSet<RptCaregiverPerformance> RptCaregiverPerformances { get; set; }

    public virtual DbSet<RptFinancialActivity> RptFinancialActivities { get; set; }

    public virtual DbSet<RptStockStatus> RptStockStatuses { get; set; }

    public virtual DbSet<RptUserCenterDetail> RptUserCenterDetails { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<StockInventory> StockInventories { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<StockTransfer> StockTransfers { get; set; }

    public virtual DbSet<SystemErrorLog> SystemErrorLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserCenterAssignment> UserCenterAssignments { get; set; }

    public virtual DbSet<UserLastSelectedCenter> UserLastSelectedCenters { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Applicat__3214EC071D18FD3B");

            entity.HasIndex(e => new { e.Category, e.Action }, "IX_ApplicationLogs_Category_Action");

            entity.HasIndex(e => e.HospitalCenterId, "IX_ApplicationLogs_Center");

            entity.HasIndex(e => e.LogLevel, "IX_ApplicationLogs_Level");

            entity.HasIndex(e => e.Timestamp, "IX_ApplicationLogs_Timestamp");

            entity.HasIndex(e => new { e.UserId, e.Timestamp }, "IX_ApplicationLogs_UserId_Timestamp");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.LogLevel).HasMaxLength(20);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC0710860B37");

            entity.ToTable("AuditLog");

            entity.HasIndex(e => e.ActionDate, "IX_AuditLog_ActionDate");

            entity.HasIndex(e => e.HospitalCenterId, "IX_AuditLog_Center");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_AuditLog_EntityType_EntityId");

            entity.HasIndex(e => new { e.UserId, e.ActionDate }, "IX_AuditLog_User_ActionDate");

            entity.Property(e => e.ActionDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ActionType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.HospitalCenterId)
                .HasConstraintName("FK_AuditLog_HospitalCenters");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLog_Users");
        });

        modelBuilder.Entity<CareEpisode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CareEpis__3214EC071A2F538F");

            entity.HasIndex(e => e.PrimaryCaregiver, "IX_CareEpisodes_Caregiver");

            entity.HasIndex(e => e.HospitalCenterId, "IX_CareEpisodes_Center");

            entity.HasIndex(e => e.DiagnosisId, "IX_CareEpisodes_Diagnosis");

            entity.HasIndex(e => e.PatientId, "IX_CareEpisodes_Patient");

            entity.HasIndex(e => e.Status, "IX_CareEpisodes_Status");

            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.EpisodeStartDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.InterruptionReason).HasMaxLength(500);
            entity.Property(e => e.RemainingBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.CareEpisodes)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareEpisodes_Diagnoses");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.CareEpisodes)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareEpisodes_HospitalCenters");

            entity.HasOne(d => d.Patient).WithMany(p => p.CareEpisodes)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareEpisodes_Patients");

            entity.HasOne(d => d.PrimaryCaregiverNavigation).WithMany(p => p.CareEpisodes)
                .HasForeignKey(d => d.PrimaryCaregiver)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareEpisodes_PrimaryCaregiver");
        });

        modelBuilder.Entity<CareService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CareServ__3214EC0772F03996");

            entity.HasIndex(e => e.AdministeredBy, "IX_CareServices_AdministeredBy");

            entity.HasIndex(e => e.ServiceDate, "IX_CareServices_Date");

            entity.HasIndex(e => e.CareEpisodeId, "IX_CareServices_Episode");

            entity.HasIndex(e => e.CareTypeId, "IX_CareServices_Type");

            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ServiceDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.AdministeredByNavigation).WithMany(p => p.CareServices)
                .HasForeignKey(d => d.AdministeredBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareServices_AdministeredBy");

            entity.HasOne(d => d.CareEpisode).WithMany(p => p.CareServices)
                .HasForeignKey(d => d.CareEpisodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareServices_CareEpisodes");

            entity.HasOne(d => d.CareType).WithMany(p => p.CareServices)
                .HasForeignKey(d => d.CareTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareServices_CareTypes");
        });

        modelBuilder.Entity<CareServiceProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CareServ__3214EC07CA96F478");

            entity.HasIndex(e => e.ProductId, "IX_CareServiceProducts_Product");

            entity.HasIndex(e => e.CareServiceId, "IX_CareServiceProducts_Service");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.QuantityUsed).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CareService).WithMany(p => p.CareServiceProducts)
                .HasForeignKey(d => d.CareServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareServiceProducts_CareServices");

            entity.HasOne(d => d.Product).WithMany(p => p.CareServiceProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CareServiceProducts_Products");
        });

        modelBuilder.Entity<CareType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CareType__3214EC07546655E1");

            entity.HasIndex(e => e.Name, "IX_CareTypes_Name");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<CashHandover>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CashHand__3214EC074143D8EE");

            entity.HasIndex(e => e.HospitalCenterId, "IX_CashHandovers_Center");

            entity.HasIndex(e => e.HandoverDate, "IX_CashHandovers_Date");

            entity.HasIndex(e => e.FinancierId, "IX_CashHandovers_Financier");

            entity.HasIndex(e => e.HandedOverBy, "IX_CashHandovers_HandedOverBy");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.HandoverAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HandoverDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.RemainingCashAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalCashAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Financier).WithMany(p => p.CashHandovers)
                .HasForeignKey(d => d.FinancierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CashHandovers_Financiers");

            entity.HasOne(d => d.HandedOverByNavigation).WithMany(p => p.CashHandovers)
                .HasForeignKey(d => d.HandedOverBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CashHandovers_HandedOverBy");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.CashHandovers)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CashHandovers_HospitalCenters");
        });

        modelBuilder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Diagnose__3214EC073DE10672");

            entity.HasIndex(e => e.HospitalCenterId, "IX_Diagnoses_Center");

            entity.HasIndex(e => e.DiagnosisDate, "IX_Diagnoses_Date");

            entity.HasIndex(e => e.DiagnosedBy, "IX_Diagnoses_DiagnosedBy");

            entity.HasIndex(e => e.PatientId, "IX_Diagnoses_Patient");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiagnosisCode).HasMaxLength(50);
            entity.Property(e => e.DiagnosisDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DiagnosisName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Severity).HasMaxLength(50);

            entity.HasOne(d => d.DiagnosedByNavigation).WithMany(p => p.Diagnoses)
                .HasForeignKey(d => d.DiagnosedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diagnoses_DiagnosedBy");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.Diagnoses)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diagnoses_HospitalCenters");

            entity.HasOne(d => d.Patient).WithMany(p => p.Diagnoses)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diagnoses_Patients");
        });

        modelBuilder.Entity<Examination>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Examinat__3214EC0746AFD8AB");

            entity.HasIndex(e => e.HospitalCenterId, "IX_Examinations_Center");

            entity.HasIndex(e => e.CareEpisodeId, "IX_Examinations_Episode");

            entity.HasIndex(e => e.PatientId, "IX_Examinations_Patient");

            entity.HasIndex(e => e.RequestedBy, "IX_Examinations_RequestedBy");

            entity.HasIndex(e => e.ScheduledDate, "IX_Examinations_ScheduledDate");

            entity.HasIndex(e => e.Status, "IX_Examinations_Status");

            entity.HasIndex(e => e.ExaminationTypeId, "IX_Examinations_Type");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FinalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.RequestDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Requested");

            entity.HasOne(d => d.CareEpisode).WithMany(p => p.Examinations)
                .HasForeignKey(d => d.CareEpisodeId)
                .HasConstraintName("FK_Examinations_CareEpisodes");

            entity.HasOne(d => d.ExaminationType).WithMany(p => p.Examinations)
                .HasForeignKey(d => d.ExaminationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Examinations_ExaminationTypes");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.Examinations)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Examinations_HospitalCenters");

            entity.HasOne(d => d.Patient).WithMany(p => p.Examinations)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Examinations_Patients");

            entity.HasOne(d => d.PerformedByNavigation).WithMany(p => p.ExaminationPerformedByNavigations)
                .HasForeignKey(d => d.PerformedBy)
                .HasConstraintName("FK_Examinations_PerformedBy");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.ExaminationRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Examinations_RequestedBy");
        });

        modelBuilder.Entity<ExaminationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Examinat__3214EC07397A5020");

            entity.HasIndex(e => e.ExaminationId, "IX_ExaminationResults_Examination");

            entity.HasIndex(e => e.ReportedBy, "IX_ExaminationResults_ReportedBy");

            entity.HasIndex(e => e.ExaminationId, "UQ_ExaminationResults_Examination").IsUnique();

            entity.Property(e => e.AttachmentPath).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ReportDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ResultNotes).HasMaxLength(2000);

            entity.HasOne(d => d.Examination).WithOne(p => p.ExaminationResult)
                .HasForeignKey<ExaminationResult>(d => d.ExaminationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExaminationResults_Examinations");

            entity.HasOne(d => d.ReportedByNavigation).WithMany(p => p.ExaminationResults)
                .HasForeignKey(d => d.ReportedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExaminationResults_ReportedBy");
        });

        modelBuilder.Entity<ExaminationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Examinat__3214EC07B0402B59");

            entity.HasIndex(e => e.Category, "IX_ExaminationTypes_Category");

            entity.HasIndex(e => e.Name, "IX_ExaminationTypes_Name");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SubcontractorPrice).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Financier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Financie__3214EC0756F00E04");

            entity.HasIndex(e => e.HospitalCenterId, "IX_Financiers_Center");

            entity.HasIndex(e => e.Name, "IX_Financiers_Name");

            entity.Property(e => e.ContactInfo).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.Financiers)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Financiers_HospitalCenters");
        });

        modelBuilder.Entity<HospitalCenter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Hospital__3214EC0779814485");

            entity.HasIndex(e => e.IsActive, "IX_HospitalCenters_IsActive");

            entity.HasIndex(e => e.Name, "IX_HospitalCenters_Name");

            entity.HasIndex(e => e.Name, "UQ__Hospital__737584F6A138CC20").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Patients__3214EC07419DC0A0");

            entity.HasIndex(e => e.Email, "IX_Patients_Email");

            entity.HasIndex(e => e.IsActive, "IX_Patients_IsActive");

            entity.HasIndex(e => new { e.LastName, e.FirstName }, "IX_Patients_LastName_FirstName");

            entity.HasIndex(e => e.PhoneNumber, "IX_Patients_PhoneNumber");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Allergies).HasMaxLength(1000);
            entity.Property(e => e.BloodType).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC07D1F33010");

            entity.HasIndex(e => e.HospitalCenterId, "IX_Payments_Center");

            entity.HasIndex(e => e.PaymentDate, "IX_Payments_Date");

            entity.HasIndex(e => e.PaymentMethodId, "IX_Payments_Method");

            entity.HasIndex(e => e.PatientId, "IX_Payments_Patient");

            entity.HasIndex(e => e.ReceivedBy, "IX_Payments_ReceivedBy");

            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "IX_Payments_Reference");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.Payments)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_HospitalCenters");

            entity.HasOne(d => d.Patient).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("FK_Payments_Patients");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_PaymentMethods");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_ReceivedBy");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PaymentM__3214EC0783AEC645");

            entity.HasIndex(e => e.Name, "IX_PaymentMethods_Name");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Prescrip__3214EC07A400B9C8");

            entity.HasIndex(e => e.HospitalCenterId, "IX_Prescriptions_Center");

            entity.HasIndex(e => e.DiagnosisId, "IX_Prescriptions_Diagnosis");

            entity.HasIndex(e => e.CareEpisodeId, "IX_Prescriptions_Episode");

            entity.HasIndex(e => e.PatientId, "IX_Prescriptions_Patient");

            entity.HasIndex(e => e.PrescribedBy, "IX_Prescriptions_PrescribedBy");

            entity.HasIndex(e => e.Status, "IX_Prescriptions_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Instructions).HasMaxLength(1000);
            entity.Property(e => e.PrescriptionDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.CareEpisode).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.CareEpisodeId)
                .HasConstraintName("FK_Prescriptions_CareEpisodes");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_Prescriptions_Diagnoses");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_HospitalCenters");

            entity.HasOne(d => d.Patient).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_Patients");

            entity.HasOne(d => d.PrescribedByNavigation).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.PrescribedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_PrescribedBy");
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Prescrip__3214EC07C9FA6EE6");

            entity.HasIndex(e => e.PrescriptionId, "IX_PrescriptionItems_Prescription");

            entity.HasIndex(e => e.ProductId, "IX_PrescriptionItems_Product");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.Duration).HasMaxLength(100);
            entity.Property(e => e.Frequency).HasMaxLength(100);
            entity.Property(e => e.Instructions).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Prescription).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrescriptionItems_Prescriptions");

            entity.HasOne(d => d.Product).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrescriptionItems_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC073FB7064F");

            entity.HasIndex(e => e.ProductCategoryId, "IX_Products_Category");

            entity.HasIndex(e => e.IsActive, "IX_Products_IsActive");

            entity.HasIndex(e => e.Name, "IX_Products_Name");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(50);

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_ProductCategories");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductC__3214EC07E24150E0");

            entity.HasIndex(e => e.Name, "IX_ProductCategories_Name");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<RptActiveSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__rpt_Acti__3214EC07BBA2A980");

            entity.ToTable("rpt_ActiveSessions");

            entity.HasIndex(e => e.ReportGeneratedAt, "IX_rpt_ActiveSessions_Generated");

            entity.HasIndex(e => e.SessionId, "IX_rpt_ActiveSessions_Session");

            entity.HasIndex(e => e.UserId, "IX_rpt_ActiveSessions_User");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrentHospitalCenter).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.ReportGeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UserName).HasMaxLength(201);
        });

        modelBuilder.Entity<RptCaregiverPerformance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__rpt_Care__3214EC0780BDA9B9");

            entity.ToTable("rpt_CaregiverPerformance");

            entity.HasIndex(e => new { e.HospitalCenterId, e.ReportDate }, "IX_rpt_CaregiverPerformance_Center_Date");

            entity.HasIndex(e => e.ReportGeneratedAt, "IX_rpt_CaregiverPerformance_Generated");

            entity.HasIndex(e => new { e.UserId, e.ReportDate }, "IX_rpt_CaregiverPerformance_User_Date");

            entity.Property(e => e.CaregiverName).HasMaxLength(201);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.HospitalCenterName).HasMaxLength(200);
            entity.Property(e => e.ReportGeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TotalRevenueGenerated).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<RptFinancialActivity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__rpt_Fina__3214EC07DE08825B");

            entity.ToTable("rpt_FinancialActivity");

            entity.HasIndex(e => new { e.HospitalCenterId, e.ReportDate }, "IX_rpt_FinancialActivity_Center_Date");

            entity.HasIndex(e => e.ReportDate, "IX_rpt_FinancialActivity_Date");

            entity.HasIndex(e => e.ReportGeneratedAt, "IX_rpt_FinancialActivity_Generated");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.HospitalCenterName).HasMaxLength(200);
            entity.Property(e => e.ReportGeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TotalCareRevenue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalCashPayments).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalExaminationRevenue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalMobilePayments).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalSales).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<RptStockStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__rpt_Stoc__3214EC0702107E28");

            entity.ToTable("rpt_StockStatus");

            entity.HasIndex(e => e.HospitalCenterId, "IX_rpt_StockStatus_Center");

            entity.HasIndex(e => e.ReportGeneratedAt, "IX_rpt_StockStatus_Generated");

            entity.HasIndex(e => e.ProductId, "IX_rpt_StockStatus_Product");

            entity.HasIndex(e => e.StockStatus, "IX_rpt_StockStatus_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrentQuantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HospitalCenterName).HasMaxLength(200);
            entity.Property(e => e.MaximumThreshold).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinimumThreshold).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductCategory).HasMaxLength(200);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ReportGeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.StockStatus).HasMaxLength(50);
        });

        modelBuilder.Entity<RptUserCenterDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__rpt_User__3214EC07DA7C6436");

            entity.ToTable("rpt_UserCenterDetails");

            entity.HasIndex(e => e.HospitalCenterId, "IX_rpt_UserCenterDetails_Center");

            entity.HasIndex(e => e.ReportGeneratedAt, "IX_rpt_UserCenterDetails_Generated");

            entity.HasIndex(e => e.UserId, "IX_rpt_UserCenterDetails_User");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.HospitalCenterName).HasMaxLength(200);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ReportGeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.RoleType).HasMaxLength(50);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sales__3214EC073A5001B1");

            entity.HasIndex(e => e.HospitalCenterId, "IX_Sales_Center");

            entity.HasIndex(e => e.SaleDate, "IX_Sales_Date");

            entity.HasIndex(e => e.PatientId, "IX_Sales_Patient");

            entity.HasIndex(e => e.PaymentStatus, "IX_Sales_PaymentStatus");

            entity.HasIndex(e => e.SaleNumber, "IX_Sales_SaleNumber");

            entity.HasIndex(e => e.SoldBy, "IX_Sales_SoldBy");

            entity.HasIndex(e => e.SaleNumber, "UQ__Sales__5C16AF1D8CFD6DA4").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SaleDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SaleNumber).HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.Sales)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sales_HospitalCenters");

            entity.HasOne(d => d.Patient).WithMany(p => p.Sales)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("FK_Sales_Patients");

            entity.HasOne(d => d.SoldByNavigation).WithMany(p => p.Sales)
                .HasForeignKey(d => d.SoldBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sales_SoldBy");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SaleItem__3214EC0743215004");

            entity.HasIndex(e => e.ProductId, "IX_SaleItems_Product");

            entity.HasIndex(e => e.SaleId, "IX_SaleItems_Sale");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SaleItems_Products");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SaleItems_Sales");
        });

        modelBuilder.Entity<StockInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StockInv__3214EC07F0A829FF");

            entity.ToTable("StockInventory");

            entity.HasIndex(e => e.HospitalCenterId, "IX_StockInventory_Center");

            entity.HasIndex(e => new { e.CurrentQuantity, e.MinimumThreshold }, "IX_StockInventory_LowStock");

            entity.HasIndex(e => e.ProductId, "IX_StockInventory_Product");

            entity.HasIndex(e => new { e.ProductId, e.HospitalCenterId }, "UQ_StockInventory_Product_Center").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrentQuantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaximumThreshold).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinimumThreshold).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.StockInventories)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockInventory_HospitalCenters");

            entity.HasOne(d => d.Product).WithMany(p => p.StockInventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockInventory_Products");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StockMov__3214EC075DC0FC8C");

            entity.HasIndex(e => new { e.HospitalCenterId, e.MovementDate }, "IX_StockMovements_Center_Date");

            entity.HasIndex(e => new { e.ProductId, e.MovementDate }, "IX_StockMovements_Product_Date");

            entity.HasIndex(e => e.MovementType, "IX_StockMovements_Type");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.MovementDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.MovementType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenceType).HasMaxLength(50);

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.HospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_HospitalCenters");

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Products");
        });

        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StockTra__3214EC07A9A375F4");

            entity.HasIndex(e => e.FromHospitalCenterId, "IX_StockTransfers_FromCenter");

            entity.HasIndex(e => e.ProductId, "IX_StockTransfers_Product");

            entity.HasIndex(e => e.Status, "IX_StockTransfers_Status");

            entity.HasIndex(e => e.ToHospitalCenterId, "IX_StockTransfers_ToCenter");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RequestDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransferReason).HasMaxLength(500);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockTransfers)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_StockTransfers_ApprovedBy");

            entity.HasOne(d => d.FromHospitalCenter).WithMany(p => p.StockTransferFromHospitalCenters)
                .HasForeignKey(d => d.FromHospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransfers_FromCenter");

            entity.HasOne(d => d.Product).WithMany(p => p.StockTransfers)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransfers_Products");

            entity.HasOne(d => d.ToHospitalCenter).WithMany(p => p.StockTransferToHospitalCenters)
                .HasForeignKey(d => d.ToHospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransfers_ToCenter");
        });

        modelBuilder.Entity<SystemErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemEr__3214EC0714DAC910");

            entity.HasIndex(e => e.IsResolved, "IX_SystemErrorLogs_IsResolved");

            entity.HasIndex(e => e.Severity, "IX_SystemErrorLogs_Severity");

            entity.HasIndex(e => e.Source, "IX_SystemErrorLogs_Source");

            entity.HasIndex(e => e.Timestamp, "IX_SystemErrorLogs_Timestamp");

            entity.Property(e => e.ErrorId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ErrorType).HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.Source).HasMaxLength(200);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07FA2AA6AB");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.IsActive, "IX_Users_IsActive");

            entity.HasIndex(e => new { e.LastName, e.FirstName }, "IX_Users_LastName_FirstName");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105349F2240C5").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<UserCenterAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserCent__3214EC0701D12AA3");

            entity.HasIndex(e => new { e.HospitalCenterId, e.IsActive }, "IX_UserCenterAssignments_Center_Active");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_UserCenterAssignments_User_Active");

            entity.Property(e => e.AssignmentStartDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoleType).HasMaxLength(50);

            entity.HasOne(d => d.HospitalCenter).WithMany(p => p.UserCenterAssignments)
                .HasForeignKey(d => d.HospitalCenterId)
                .HasConstraintName("FK_UserCenterAssignments_HospitalCenters");

            entity.HasOne(d => d.User).WithMany(p => p.UserCenterAssignments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserCenterAssignments_Users");
        });

        modelBuilder.Entity<UserLastSelectedCenter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserLast__3214EC07D2C56DEB");

            entity.HasIndex(e => e.UserId, "IX_UserLastSelectedCenters_User");

            entity.HasIndex(e => e.UserId, "UQ_UserLastSelectedCenters_User").IsUnique();

            entity.Property(e => e.LastSelectionDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.LastSelectedHospitalCenter).WithMany(p => p.UserLastSelectedCenters)
                .HasForeignKey(d => d.LastSelectedHospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserLastSelectedCenters_HospitalCenters");

            entity.HasOne(d => d.User).WithOne(p => p.UserLastSelectedCenter)
                .HasForeignKey<UserLastSelectedCenter>(d => d.UserId)
                .HasConstraintName("FK_UserLastSelectedCenters_Users");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserSess__3214EC07A7A66804");

            entity.HasIndex(e => e.LoginTime, "IX_UserSessions_LoginTime");

            entity.HasIndex(e => e.SessionToken, "IX_UserSessions_Token");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_UserSessions_User_Active");

            entity.HasIndex(e => e.SessionToken, "UQ__UserSess__46BDD124D82ADF67").IsUnique();

            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LoginTime).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SessionToken).HasMaxLength(256);

            entity.HasOne(d => d.CurrentHospitalCenter).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.CurrentHospitalCenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSessions_HospitalCenters");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserSessions_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
