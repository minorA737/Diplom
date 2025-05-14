using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.Models;

public partial class PostgresContext : DbContext
{
    public PostgresContext()
    {
    }

    public PostgresContext(DbContextOptions<PostgresContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DesignDocumentation> DesignDocumentations { get; set; }

    public virtual DbSet<MaterialsManagement> MaterialsManagements { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderPosition> OrderPositions { get; set; }

    public virtual DbSet<OrderPositionsView> OrderPositionsViews { get; set; }

    public virtual DbSet<ProductionDetail> ProductionDetails { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskCategory> TaskCategories { get; set; }

    public virtual DbSet<TaskDetailsView> TaskDetailsViews { get; set; }

    public virtual DbSet<TaskDevelopmentDetail> TaskDevelopmentDetails { get; set; }

    public virtual DbSet<TaskHistory> TaskHistories { get; set; }

    public virtual DbSet<TaskTemplate> TaskTemplates { get; set; }

    public virtual DbSet<TaskUser> TaskUsers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserDepartment> UserDepartments { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Сначала пытаемся загрузить настройки из файла
            var connectionString = LoadConnectionStringFromFile();
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    // В файле Models/PostgresContext.cs изменить метод LoadConnectionStringFromFile:

    private string LoadConnectionStringFromFile()
    {
        string defaultConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123";

        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ManufactPlanner",
                "database-settings.json"
            );

            if (File.Exists(settingsPath))
            {
                var settingsText = File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settingsText);

                var customConnectionString = $"Host={settings["Host"]};Port={settings["Port"]};Database={settings["Database"]};Username={settings["Username"]};Password={settings["Password"]}";

                // Тестируем подключение с пользовательскими настройками
                try
                {
                    using var testContext = new PostgresContext();
                    using var connection = new Npgsql.NpgsqlConnection(customConnectionString);
                    connection.Open();
                    connection.Close();

                    return customConnectionString;
                }
                catch (Exception ex)
                {
                    // Если подключение неудачно, логируем ошибку и используем значения по умолчанию
                    System.Diagnostics.Debug.WriteLine($"Ошибка подключения с пользовательскими настройками: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("Переключение на настройки по умолчанию");

                    // Опционально: можно удалить файл с неверными настройками
                    try
                    {
                        File.Delete(settingsPath);
                        System.Diagnostics.Debug.WriteLine("Файл с неверными настройками удален");
                    }
                    catch
                    {
                        // Игнорируем ошибки удаления
                    }

                    return defaultConnectionString;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке настроек подключения: {ex.Message}");
        }

        // Возвращаем значения по умолчанию
        return defaultConnectionString;
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pg_catalog", "adminpack")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attachments_pkey");

            entity.ToTable("attachments");

            entity.HasIndex(e => e.TaskId, "idx_attachments_task_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileContent)
                .HasComment("Содержимое файла в бинарном формате (PDF, документы и пр.)")
                .HasColumnName("file_content");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.FileType)
                .HasMaxLength(255)
                .HasColumnName("file_type");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Task).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachments_task_id_fkey");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("attachments_uploaded_by_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.ToTable("comments");

            entity.HasIndex(e => e.TaskId, "idx_comments_task_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.Comments)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comments_task_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("comments_user_id_fkey");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");

            entity.ToTable("customers");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(100)
                .HasColumnName("contact_person");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("departments_pkey");

            entity.ToTable("departments");

            entity.HasIndex(e => e.Name, "departments_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<DesignDocumentation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("design_documentation_pkey");

            entity.ToTable("design_documentation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArtDesignDate).HasColumnName("art_design_date");
            entity.Property(e => e.ComponentListDate).HasColumnName("component_list_date");
            entity.Property(e => e.CuttingSubcontractDate).HasColumnName("cutting_subcontract_date");
            entity.Property(e => e.CuttingWorkshopDate).HasColumnName("cutting_workshop_date");
            entity.Property(e => e.DesignDocsDate).HasColumnName("design_docs_date");
            entity.Property(e => e.ElectronicComponentListDate).HasColumnName("electronic_component_list_date");
            entity.Property(e => e.ElectronicDesignDocsDate).HasColumnName("electronic_design_docs_date");
            entity.Property(e => e.ElectronicSoftwareDate).HasColumnName("electronic_software_date");
            entity.Property(e => e.FurnitureSubcontractDate).HasColumnName("furniture_subcontract_date");
            entity.Property(e => e.ManualDate).HasColumnName("manual_date");
            entity.Property(e => e.MaterialsNormsDate).HasColumnName("materials_norms_date");
            entity.Property(e => e.MechanicalProcessingDate).HasColumnName("mechanical_processing_date");
            entity.Property(e => e.OperationManualDate).HasColumnName("operation_manual_date");
            entity.Property(e => e.OrderPositionId).HasColumnName("order_position_id");
            entity.Property(e => e.PassportDate).HasColumnName("passport_date");
            entity.Property(e => e.Printing3dDate).HasColumnName("printing_3d_date");
            entity.Property(e => e.PrintingFileDate).HasColumnName("printing_file_date");
            entity.Property(e => e.ProductCompositionDate).HasColumnName("product_composition_date");
            entity.Property(e => e.SoftwareDate).HasColumnName("software_date");
            entity.Property(e => e.TechnicalTaskDate).HasColumnName("technical_task_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.OrderPosition).WithMany(p => p.DesignDocumentations)
                .HasForeignKey(d => d.OrderPositionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("design_documentation_order_position_id_fkey");
        });

        modelBuilder.Entity<MaterialsManagement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("materials_management_pkey");

            entity.ToTable("materials_management");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CuttingOrderDate).HasColumnName("cutting_order_date");
            entity.Property(e => e.CuttingProvisionDate).HasColumnName("cutting_provision_date");
            entity.Property(e => e.MaterialCompletionDeadline).HasColumnName("material_completion_deadline");
            entity.Property(e => e.MaterialOrderDate).HasColumnName("material_order_date");
            entity.Property(e => e.MaterialProvisionDate).HasColumnName("material_provision_date");
            entity.Property(e => e.MaterialSelectionDeadline).HasColumnName("material_selection_deadline");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderPositionId).HasColumnName("order_position_id");
            entity.Property(e => e.ReoOrderDate).HasColumnName("reo_order_date");
            entity.Property(e => e.ReoProvisionDate).HasColumnName("reo_provision_date");
            entity.Property(e => e.TkiDeliveryDate).HasColumnName("tki_delivery_date");
            entity.Property(e => e.TkiOrderDate).HasColumnName("tki_order_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.OrderPosition).WithMany(p => p.MaterialsManagements)
                .HasForeignKey(d => d.OrderPositionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("materials_management_order_position_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "idx_notifications_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.LinkTo).HasColumnName("link_to");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.HasIndex(e => e.OrderNumber, "orders_order_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcceptanceDate).HasColumnName("acceptance_date");
            entity.Property(e => e.ContractDeadline).HasColumnName("contract_deadline");
            entity.Property(e => e.ContractQuantity)
                .HasDefaultValue(1)
                .HasColumnName("contract_quantity");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Customer)
                .HasMaxLength(255)
                .HasColumnName("customer");
            entity.Property(e => e.DeliveryDeadline).HasColumnName("delivery_deadline");
            entity.Property(e => e.HasInstallation)
                .HasDefaultValue(false)
                .HasColumnName("has_installation");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.ShippingDate).HasColumnName("shipping_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Активен'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(15, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("orders_created_by_fkey");

            entity.HasMany(d => d.Customers).WithMany(p => p.Orders)
                .UsingEntity<Dictionary<string, object>>(
                    "OrderCustomer",
                    r => r.HasOne<Customer>().WithMany()
                        .HasForeignKey("CustomerId")
                        .HasConstraintName("order_customers_customer_id_fkey"),
                    l => l.HasOne<Order>().WithMany()
                        .HasForeignKey("OrderId")
                        .HasConstraintName("order_customers_order_id_fkey"),
                    j =>
                    {
                        j.HasKey("OrderId", "CustomerId").HasName("order_customers_pkey");
                        j.ToTable("order_customers");
                        j.IndexerProperty<int>("OrderId").HasColumnName("order_id");
                        j.IndexerProperty<int>("CustomerId").HasColumnName("customer_id");
                    });
        });

        modelBuilder.Entity<OrderPosition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_positions_pkey");

            entity.ToTable("order_positions");

            entity.HasIndex(e => e.OrderId, "idx_order_positions_order_id");

            entity.HasIndex(e => new { e.OrderId, e.PositionNumber }, "order_positions_order_id_position_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentStatus)
                .HasMaxLength(100)
                .HasDefaultValueSql("'В очереди'::character varying")
                .HasColumnName("current_status");
            entity.Property(e => e.DevelopmentType)
                .HasMaxLength(50)
                .HasColumnName("development_type");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PositionNumber)
                .HasMaxLength(50)
                .HasColumnName("position_number");
            entity.Property(e => e.Price)
                .HasPrecision(15, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(15, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkflowTime)
                .HasMaxLength(50)
                .HasColumnName("workflow_time");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderPositions)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_positions_order_id_fkey");
        });

        modelBuilder.Entity<OrderPositionsView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("order_positions_view");

            entity.Property(e => e.AcceptanceDate).HasColumnName("acceptance_date");
            entity.Property(e => e.ContractDeadline).HasColumnName("contract_deadline");
            entity.Property(e => e.ContractQuantity).HasColumnName("contract_quantity");
            entity.Property(e => e.CurrentStatus)
                .HasMaxLength(100)
                .HasColumnName("current_status");
            entity.Property(e => e.Customer)
                .HasMaxLength(255)
                .HasColumnName("customer");
            entity.Property(e => e.DeliveryDeadline).HasColumnName("delivery_deadline");
            entity.Property(e => e.DevelopmentType)
                .HasMaxLength(50)
                .HasColumnName("development_type");
            entity.Property(e => e.HasInstallation).HasColumnName("has_installation");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasColumnName("order_status");
            entity.Property(e => e.OrderTotalPrice)
                .HasPrecision(15, 2)
                .HasColumnName("order_total_price");
            entity.Property(e => e.PositionNumber)
                .HasMaxLength(50)
                .HasColumnName("position_number");
            entity.Property(e => e.Price)
                .HasPrecision(15, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ShippingDate).HasColumnName("shipping_date");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(15, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.WorkflowTime)
                .HasMaxLength(50)
                .HasColumnName("workflow_time");
        });

        modelBuilder.Entity<ProductionDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("production_details_pkey");

            entity.ToTable("production_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcceptanceDate).HasColumnName("acceptance_date");
            entity.Property(e => e.DebuggingDate).HasColumnName("debugging_date");
            entity.Property(e => e.MasterName)
                .HasMaxLength(100)
                .HasColumnName("master_name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.OrderPositionId).HasColumnName("order_position_id");
            entity.Property(e => e.PackagingDate).HasColumnName("packaging_date");
            entity.Property(e => e.ProductionDate).HasColumnName("production_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.OrderPosition).WithMany(p => p.ProductionDetails)
                .HasForeignKey(d => d.OrderPositionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("production_details_order_position_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tasks_pkey");

            entity.ToTable("tasks");

            entity.HasIndex(e => e.AssigneeId, "idx_tasks_assignee");

            entity.HasIndex(e => e.EndDate, "idx_tasks_end_date");

            entity.HasIndex(e => e.Priority, "idx_tasks_priority");

            entity.HasIndex(e => e.StartDate, "idx_tasks_start_date");

            entity.HasIndex(e => e.Status, "idx_tasks_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            entity.Property(e => e.CoAssignees).HasColumnName("co_assignees");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DebuggingStatus)
                .HasMaxLength(255)
                .HasColumnName("debugging_status");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderPositionId).HasColumnName("order_position_id");
            entity.Property(e => e.Priority)
                .HasDefaultValue(3)
                .HasColumnName("priority");
            entity.Property(e => e.Stage)
                .HasMaxLength(100)
                .HasColumnName("stage");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'В очереди'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Assignee).WithMany(p => p.TaskAssignees)
                .HasForeignKey(d => d.AssigneeId)
                .HasConstraintName("tasks_assignee_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TaskCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("tasks_created_by_fkey");

            entity.HasOne(d => d.OrderPosition).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.OrderPositionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("tasks_order_position_id_fkey");
        });

        modelBuilder.Entity<TaskCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_categories_pkey");

            entity.ToTable("task_categories");

            entity.HasIndex(e => e.Name, "task_categories_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TaskDetailsView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("task_details_view");

            entity.Property(e => e.AssigneeName).HasColumnName("assignee_name");
            entity.Property(e => e.CoAssignees).HasColumnName("co_assignees");
            entity.Property(e => e.Customer)
                .HasMaxLength(255)
                .HasColumnName("customer");
            entity.Property(e => e.DebuggingStatus)
                .HasMaxLength(255)
                .HasColumnName("debugging_status");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FirmwareVersion)
                .HasMaxLength(50)
                .HasColumnName("firmware_version");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MountingSchemeStatus)
                .HasMaxLength(100)
                .HasColumnName("mounting_scheme_status");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.PositionNumber)
                .HasMaxLength(50)
                .HasColumnName("position_number");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            entity.Property(e => e.SchematicStatus)
                .HasMaxLength(100)
                .HasColumnName("schematic_status");
            entity.Property(e => e.Stage)
                .HasMaxLength(100)
                .HasColumnName("stage");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
        });

        modelBuilder.Entity<TaskDevelopmentDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_development_details_pkey");

            entity.ToTable("task_development_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DebuggingNotes).HasColumnName("debugging_notes");
            entity.Property(e => e.FirmwareVersion)
                .HasMaxLength(50)
                .HasColumnName("firmware_version");
            entity.Property(e => e.MountingSchemeStatus)
                .HasMaxLength(100)
                .HasColumnName("mounting_scheme_status");
            entity.Property(e => e.SchematicStatus)
                .HasMaxLength(100)
                .HasColumnName("schematic_status");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskDevelopmentDetails)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("task_development_details_task_id_fkey");
        });

        modelBuilder.Entity<TaskHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_history_pkey");

            entity.ToTable("task_history");

            entity.HasIndex(e => e.TaskId, "idx_task_history_task_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.FieldName)
                .HasMaxLength(50)
                .HasColumnName("field_name");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.TaskId).HasColumnName("task_id");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.TaskHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("task_history_changed_by_fkey");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskHistories)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("task_history_task_id_fkey");
        });

        modelBuilder.Entity<TaskTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_templates_pkey");

            entity.ToTable("task_templates");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DefaultPriority)
                .HasDefaultValue(3)
                .HasColumnName("default_priority");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedDuration).HasColumnName("estimated_duration");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.TaskTemplates)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("task_templates_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TaskTemplates)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("task_templates_created_by_fkey");
        });

        modelBuilder.Entity<TaskUser>(entity =>
        {
            entity.HasKey(e => new { e.TaskId, e.UserId }).HasName("task_users_pkey");

            entity.ToTable("task_users");

            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'assignee'::character varying")
                .HasColumnName("role");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskUsers)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("task_users_task_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TaskUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("task_users_user_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLogin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("user_roles_role_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("user_roles_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("user_roles_pkey");
                        j.ToTable("user_roles");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<UserDepartment>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.DepartmentId }).HasName("user_departments_pkey");

            entity.ToTable("user_departments");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.IsHead)
                .HasDefaultValue(false)
                .HasColumnName("is_head");

            entity.HasOne(d => d.Department).WithMany(p => p.UserDepartments)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("user_departments_department_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserDepartments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_departments_user_id_fkey");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_settings_pkey");

            entity.ToTable("user_settings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AutoStartEnabled)
                .HasDefaultValue(false)
                .HasColumnName("auto_start_enabled");
            entity.Property(e => e.Islighttheme).HasColumnName("islighttheme");
            entity.Property(e => e.NotifyComments)
                .HasDefaultValue(true)
                .HasColumnName("notify_comments");
            entity.Property(e => e.NotifyDeadlines)
                .HasDefaultValue(true)
                .HasColumnName("notify_deadlines");
            entity.Property(e => e.NotifyDesktop)
                .HasDefaultValue(true)
                .HasColumnName("notify_desktop");
            entity.Property(e => e.NotifyEmail)
                .HasDefaultValue(true)
                .HasColumnName("notify_email");
            entity.Property(e => e.NotifyNewTasks)
                .HasDefaultValue(true)
                .HasColumnName("notify_new_tasks");
            entity.Property(e => e.NotifyStatusChanges)
                .HasDefaultValue(true)
                .HasColumnName("notify_status_changes");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSettings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_settings_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
