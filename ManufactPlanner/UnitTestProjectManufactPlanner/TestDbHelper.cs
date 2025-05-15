using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestProjectManufactPlanner.Helpers
{
    public static class TestDbHelper
    {
        /// <summary>
        /// Создает контекст InMemory базы данных для тестирования
        /// </summary>
        public static PostgresContext CreateInMemoryDbContext(string databaseName = null)
        {
            databaseName ??= Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<PostgresContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            var context = new PostgresContext(options);

            // Убеждаемся, что база данных создана
            context.Database.EnsureCreated();

            return context;
        }

        /// <summary>
        /// Создает полный набор тестовых данных
        /// </summary>
        public static void SeedFullTestData(PostgresContext context)
        {
            // Создаем роли
            var roles = CreateRoles(context);

            // Создаем пользователей
            var users = CreateUsers(context);

            // Связываем пользователей с ролями
            CreateUserRoles(context, users, roles);

            // Создаем отделы
            var departments = CreateDepartments(context);

            // Связываем пользователей с отделами
            CreateUserDepartments(context, users, departments);

            // Создаем заказчиков
            var customers = CreateCustomers(context);

            // Создаем заказы
            var orders = CreateOrders(context, users, customers);

            // Создаем позиции заказов
            var orderPositions = CreateOrderPositions(context, orders);

            // Создаем задачи
            var tasks = CreateTasks(context, orderPositions, users);

            // Создаем детали производства
            CreateProductionDetails(context, orderPositions);

            // Создаем вложения (документы)
            CreateAttachments(context, tasks, users);

            // Создаем уведомления
            CreateNotifications(context, users);

            context.SaveChanges();
        }

        /// <summary>
        /// Создает тестовые роли
        /// </summary>
        public static List<Role> CreateRoles(PostgresContext context)
        {
            var roles = new List<Role>
            {
                new Role { Id = 1, Name = "Администратор", Description = "Полный доступ к системе" },
                new Role { Id = 2, Name = "Менеджер", Description = "Управление проектами и задачами" },
                new Role { Id = 3, Name = "Исполнитель", Description = "Выполнение задач" }
            };

            context.Roles.AddRange(roles);
            context.SaveChanges();
            return roles;
        }

        /// <summary>
        /// Создает тестовых пользователей
        /// </summary>
        public static List<User> CreateUsers(PostgresContext context)
        {
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Password = "admin123",
                    FirstName = "Администратор",
                    LastName = "Системы",
                    Email = "admin@test.com",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    LastLogin = DateTime.Now.AddHours(-2)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "manager",
                    Password = "manager123",
                    FirstName = "Менеджер",
                    LastName = "Проектов",
                    Email = "manager@test.com",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    LastLogin = DateTime.Now.AddHours(-1)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "developer",
                    Password = "dev123",
                    FirstName = "Разработчик",
                    LastName = "Тестовый",
                    Email = "dev@test.com",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    LastLogin = DateTime.Now.AddMinutes(-30)
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
            return users;
        }

        /// <summary>
        /// Создает связи пользователей с ролями
        /// </summary>
        public static void CreateUserRoles(PostgresContext context, List<User> users, List<Role> roles)
        {
            var userRoles = new[]
            {
                new { UserId = users[0].Id, RoleId = roles[0].Id }, // admin - Администратор
                new { UserId = users[1].Id, RoleId = roles[1].Id }, // manager - Менеджер
                new { UserId = users[2].Id, RoleId = roles[2].Id }  // developer - Исполнитель
            };

            foreach (var ur in userRoles)
            {
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO user_roles (user_id, role_id) VALUES ({0}, {1})",
                    ur.UserId, ur.RoleId);
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Создает тестовые отделы
        /// </summary>
        public static List<Department> CreateDepartments(PostgresContext context)
        {
            var departments = new List<Department>
            {
                new Department { Id = 1, Name = "Конструкторский отдел", Description = "Разработка изделий" },
                new Department { Id = 2, Name = "Отдел РЭА", Description = "Электронная аппаратура" },
                new Department { Id = 3, Name = "Производственный отдел", Description = "Производство изделий" }
            };

            context.Departments.AddRange(departments);
            context.SaveChanges();
            return departments;
        }

        /// <summary>
        /// Создает связи пользователей с отделами
        /// </summary>
        public static void CreateUserDepartments(PostgresContext context, List<User> users, List<Department> departments)
        {
            var userDepartments = new[]
            {
                new { UserId = users[0].Id, DepartmentId = departments[0].Id, IsHead = true },
                new { UserId = users[1].Id, DepartmentId = departments[0].Id, IsHead = false },
                new { UserId = users[2].Id, DepartmentId = departments[1].Id, IsHead = false }
            };

            foreach (var ud in userDepartments)
            {
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO user_departments (user_id, department_id, is_head) VALUES ({0}, {1}, {2})",
                    ud.UserId, ud.DepartmentId, ud.IsHead);
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Создает тестовых заказчиков
        /// </summary>
        public static List<Customer> CreateCustomers(PostgresContext context)
        {
            var customers = new List<Customer>
            {
                new Customer
                {
                    Id = 1,
                    Name = "ООО Тест Компани",
                    ContactPerson = "Иван Иванов",
                    Email = "test@company.com",
                    CreatedAt = DateTime.Now.AddMonths(-2)
                },
                new Customer
                {
                    Id = 2,
                    Name = "ЗАО Пример",
                    ContactPerson = "Петр Петров",
                    Email = "example@company.com",
                    CreatedAt = DateTime.Now.AddMonths(-1)
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();
            return customers;
        }

        /// <summary>
        /// Создает тестовые заказы
        /// </summary>
        public static List<Order> CreateOrders(PostgresContext context, List<User> users, List<Customer> customers)
        {
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    OrderNumber = "ТЗ-001/24",
                    Name = "Тестовый заказ 1",
                    Customer = customers[0].Name,
                    HasInstallation = true,
                    ContractDeadline = DateOnly.FromDateTime(DateTime.Now.AddMonths(2)),
                    DeliveryDeadline = DateOnly.FromDateTime(DateTime.Now.AddMonths(1)),
                    ContractQuantity = 1,
                    TotalPrice = 100000m,
                    Status = "Активен",
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    CreatedBy = users[1].Id
                },
                new Order
                {
                    Id = 2,
                    OrderNumber = "ТЗ-002/24",
                    Name = "Тестовый заказ 2",
                    Customer = customers[1].Name,
                    HasInstallation = false,
                    ContractDeadline = DateOnly.FromDateTime(DateTime.Now.AddMonths(3)),
                    DeliveryDeadline = DateOnly.FromDateTime(DateTime.Now.AddMonths(2)),
                    ContractQuantity = 2,
                    TotalPrice = 200000m,
                    Status = "Активен",
                    CreatedAt = DateTime.Now.AddDays(-20),
                    CreatedBy = users[1].Id
                }
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();
            return orders;
        }

        /// <summary>
        /// Создает позиции заказов
        /// </summary>
        public static List<OrderPosition> CreateOrderPositions(PostgresContext context, List<Order> orders)
        {
            var orderPositions = new List<OrderPosition>
            {
                new OrderPosition
                {
                    Id = 1,
                    OrderId = orders[0].Id,
                    PositionNumber = "1",
                    ProductName = "Тестовое изделие 1",
                    Quantity = 1,
                    Price = 100000m,
                    TotalPrice = 100000m,
                    DevelopmentType = "Разработка",
                    WorkflowTime = "2-3 дня",
                    CurrentStatus = "В процессе",
                    CreatedAt = DateTime.Now.AddDays(-25)
                },
                new OrderPosition
                {
                    Id = 2,
                    OrderId = orders[1].Id,
                    PositionNumber = "1",
                    ProductName = "Тестовое изделие 2",
                    Quantity = 2,
                    Price = 100000m,
                    TotalPrice = 200000m,
                    DevelopmentType = "Покупное",
                    WorkflowTime = "1 день",
                    CurrentStatus = "В очереди",
                    CreatedAt = DateTime.Now.AddDays(-15)
                }
            };

            context.OrderPositions.AddRange(orderPositions);
            context.SaveChanges();
            return orderPositions;
        }

        /// <summary>
        /// Создает тестовые задачи
        /// </summary>
        public static List<ManufactPlanner.Models.Task> CreateTasks(PostgresContext context, List<OrderPosition> orderPositions, List<User> users)
        {
            var tasks = new List<ManufactPlanner.Models.Task>
            {
                new ManufactPlanner.Models.Task
                {
                    Id = 1,
                    OrderPositionId = orderPositions[0].Id,
                    Name = "Разработка схемы",
                    Description = "Разработать электрическую схему",
                    Priority = 2,
                    Status = "В процессе",
                    StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
                    AssigneeId = users[2].Id,
                    CreatedBy = users[1].Id,
                    Stage = "Проектирование",
                    CreatedAt = DateTime.Now.AddDays(-25),
                    UpdatedAt = DateTime.Now.AddDays(-1)
                },
                new ManufactPlanner.Models.Task
                {
                    Id = 2,
                    OrderPositionId = orderPositions[0].Id,
                    Name = "Тестирование",
                    Description = "Провести тестирование изделия",
                    Priority = 1,
                    Status = "В очереди",
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
                    AssigneeId = users[2].Id,
                    CreatedBy = users[1].Id,
                    Stage = "Тестирование",
                    CreatedAt = DateTime.Now.AddDays(-20),
                    UpdatedAt = DateTime.Now.AddDays(-5)
                },
                new ManufactPlanner.Models.Task
                {
                    Id = 3,
                    OrderPositionId = orderPositions[1].Id,
                    Name = "Подготовка документации",
                    Description = "Подготовить техническую документацию",
                    Priority = 3,
                    Status = "Готово",
                    StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-15)),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
                    AssigneeId = users[2].Id,
                    CreatedBy = users[1].Id,
                    Stage = "Документация",
                    CreatedAt = DateTime.Now.AddDays(-15),
                    UpdatedAt = DateTime.Now.AddDays(-5)
                }
            };

            context.Tasks.AddRange(tasks);
            context.SaveChanges();
            return tasks;
        }

        /// <summary>
        /// Создает детали производства
        /// </summary>
        public static void CreateProductionDetails(PostgresContext context, List<OrderPosition> orderPositions)
        {
            var productionDetails = new List<ProductionDetail>
            {
                new ProductionDetail
                {
                    Id = 1,
                    OrderPositionId = orderPositions[0].Id,
                    OrderNumber = "ПЗ-001",
                    MasterName = "Мастер Тестов",
                    ProductionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
                    DebuggingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-15)),
                    AcceptanceDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)),
                    UpdatedAt = DateTime.Now.AddDays(-1)
                },
                new ProductionDetail
                {
                    Id = 2,
                    OrderPositionId = orderPositions[1].Id,
                    OrderNumber = "ПЗ-002",
                    MasterName = "Главный Мастер",
                    ProductionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)),
                    UpdatedAt = DateTime.Now.AddDays(-1)
                }
            };

            context.ProductionDetails.AddRange(productionDetails);
            context.SaveChanges();
        }

        /// <summary>
        /// Создает вложения (документы)
        /// </summary>
        public static void CreateAttachments(PostgresContext context, List<ManufactPlanner.Models.Task> tasks, List<User> users)
        {
            var attachments = new List<Attachment>
            {
                new Attachment
                {
                    Id = 1,
                    TaskId = tasks[0].Id,
                    FileName = "схема.pdf",
                    FilePath = "/uploads/test/schema.pdf",
                    FileType = "application/pdf",
                    FileSize = 1024 * 1024, // 1MB
                    UploadedAt = DateTime.Now.AddDays(-5),
                    UploadedBy = users[2].Id
                },
                new Attachment
                {
                    Id = 2,
                    TaskId = tasks[2].Id,
                    FileName = "документация.docx",
                    FilePath = "/uploads/test/docs.docx",
                    FileType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    FileSize = 2 * 1024 * 1024, // 2MB
                    UploadedAt = DateTime.Now.AddDays(-5),
                    UploadedBy = users[2].Id
                }
            };

            context.Attachments.AddRange(attachments);
            context.SaveChanges();
        }

        /// <summary>
        /// Создает уведомления
        /// </summary>
        public static void CreateNotifications(PostgresContext context, List<User> users)
        {
            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = 1,
                    UserId = users[2].Id,
                    Title = "Новая задача",
                    Message = "Вам назначена новая задача",
                    IsRead = false,
                    CreatedAt = DateTime.Now.AddHours(-2),
                    NotificationType = "task_assigned"
                },
                new Notification
                {
                    Id = 2,
                    UserId = users[1].Id,
                    Title = "Задача выполнена",
                    Message = "Задача была выполнена",
                    IsRead = true,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    NotificationType = "task_completed"
                }
            };

            context.Notifications.AddRange(notifications);
            context.SaveChanges();
        }
    }
}