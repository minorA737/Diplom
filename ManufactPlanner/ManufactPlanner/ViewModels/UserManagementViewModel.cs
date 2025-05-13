using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using ReactiveUI;
using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.ViewModels
{
    public class UserManagementViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Коллекция всех пользователей
        private ObservableCollection<UserViewModel2> _users;

        // Выбранный пользователь для редактирования
        private UserViewModel2 _selectedUser;

        // Свойства для поиска и фильтрации
        private string _searchText = string.Empty;
        private string _selectedRoleFilter = "Все";
        private string _selectedDepartmentFilter = "Все";

        // Свойства для создания нового пользователя
        private string _newUserName = string.Empty;
        private string _newPassword = string.Empty;
        private string _newFirstName = string.Empty;
        private string _newLastName = string.Empty;
        private string _newEmail = string.Empty;

        // Свойства для редактирования выбранного пользователя
        private string _editUserName = string.Empty;
        private string _editFirstName = string.Empty;
        private string _editLastName = string.Empty;
        private string _editEmail = string.Empty;
        private bool _editIsActive = true;
        private int _editSelectedRole = 0;
        private int _editSelectedDepartment = 0;

        // Коллекции для выпадающих списков
        private ObservableCollection<string> _roleOptions;
        private ObservableCollection<string> _departmentOptions;

        // Флаги состояния
        private bool _isLoading = false;
        private bool _isCreateUserDialogOpen = false;
        private bool _isEditUserDialogOpen = false;
        private string _statusMessage = string.Empty;

        public ObservableCollection<UserViewModel2> Users
        {
            get => _users;
            set => this.RaiseAndSetIfChanged(ref _users, value);
        }

        public UserViewModel2 SelectedUser
        {
            get => _selectedUser;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedUser, value);
                LoadUserForEdit(value);
                
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                FilterUsers();
            }
        }

        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedRoleFilter, value);
                FilterUsers();
            }
        }

        public string SelectedDepartmentFilter
        {
            get => _selectedDepartmentFilter;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDepartmentFilter, value);
                FilterUsers();
            }
        }

        // Свойства для создания пользователя
        public string NewUserName
        {
            get => _newUserName;
            set => this.RaiseAndSetIfChanged(ref _newUserName, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => this.RaiseAndSetIfChanged(ref _newPassword, value);
        }

        public string NewFirstName
        {
            get => _newFirstName;
            set => this.RaiseAndSetIfChanged(ref _newFirstName, value);
        }

        public string NewLastName
        {
            get => _newLastName;
            set => this.RaiseAndSetIfChanged(ref _newLastName, value);
        }

        public string NewEmail
        {
            get => _newEmail;
            set => this.RaiseAndSetIfChanged(ref _newEmail, value);
        }

        // Свойства для редактирования пользователя
        public string EditUserName
        {
            get => _editUserName;
            set => this.RaiseAndSetIfChanged(ref _editUserName, value);
        }

        public string EditFirstName
        {
            get => _editFirstName;
            set => this.RaiseAndSetIfChanged(ref _editFirstName, value);
        }

        public string EditLastName
        {
            get => _editLastName;
            set => this.RaiseAndSetIfChanged(ref _editLastName, value);
        }

        public string EditEmail
        {
            get => _editEmail;
            set => this.RaiseAndSetIfChanged(ref _editEmail, value);
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set => this.RaiseAndSetIfChanged(ref _editIsActive, value);
        }

        public int EditSelectedRole
        {
            get => _editSelectedRole;
            set => this.RaiseAndSetIfChanged(ref _editSelectedRole, value);
        }

        public int EditSelectedDepartment
        {
            get => _editSelectedDepartment;
            set => this.RaiseAndSetIfChanged(ref _editSelectedDepartment, value);
        }

        public ObservableCollection<string> RoleOptions
        {
            get => _roleOptions;
            set => this.RaiseAndSetIfChanged(ref _roleOptions, value);
        }

        public ObservableCollection<string> DepartmentOptions
        {
            get => _departmentOptions;
            set => this.RaiseAndSetIfChanged(ref _departmentOptions, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public bool IsCreateUserDialogOpen
        {
            get => _isCreateUserDialogOpen;
            set => this.RaiseAndSetIfChanged(ref _isCreateUserDialogOpen, value);
        }

        public bool IsEditUserDialogOpen
        {
            get => _isEditUserDialogOpen;
            set => this.RaiseAndSetIfChanged(ref _isEditUserDialogOpen, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        // Команды
        public ICommand CreateUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand OpenCreateDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowDeleteConfirmationCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }

        private ObservableCollection<UserViewModel2> _allUsers;
        private bool _isConfirmDeleteDialogOpen = false;
        private UserViewModel2 _userToDelete;

        public bool IsConfirmDeleteDialogOpen
        {
            get => _isConfirmDeleteDialogOpen;
            set => this.RaiseAndSetIfChanged(ref _isConfirmDeleteDialogOpen, value);
        }


        public UserManagementViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация коллекций
            Users = new ObservableCollection<UserViewModel2>();
            RoleOptions = new ObservableCollection<string>();
            DepartmentOptions = new ObservableCollection<string>();

            // Инициализация команд
            CreateUserCommand = ReactiveCommand.CreateFromTask(CreateUserAsync);
            EditUserCommand = ReactiveCommand.CreateFromTask(SaveUserChangesAsync);
            DeleteUserCommand = ReactiveCommand.CreateFromTask<Guid>(DeleteUserAsync);
            SaveUserCommand = ReactiveCommand.CreateFromTask(SaveUserChangesAsync);
            CancelEditCommand = ReactiveCommand.Create(CancelEdit);
            OpenCreateDialogCommand = ReactiveCommand.Create(OpenCreateDialog);
            OpenEditDialogCommand = ReactiveCommand.Create<UserViewModel2>(OpenEditDialog);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
            ShowDeleteConfirmationCommand = ReactiveCommand.Create<UserViewModel2>(ShowDeleteConfirmation);
            ConfirmDeleteCommand = ReactiveCommand.CreateFromTask(ConfirmDeleteAsync);
            CancelDeleteCommand = ReactiveCommand.Create(CancelDelete);
            // Загрузка данных
            _ = InitializeAsync();
        }
        private async System.Threading.Tasks.Task InitializeAsync()
        {
            await LoadDataAsync();
            // Устанавливаем значения по умолчанию ПОСЛЕ загрузки данных
            SelectedRoleFilter = "Все";
            SelectedDepartmentFilter = "Все";
        }
        private void ShowDeleteConfirmation(UserViewModel2 user)
        {
            _userToDelete = user;
            IsConfirmDeleteDialogOpen = true;
        }

        private async System.Threading.Tasks.Task ConfirmDeleteAsync()
        {
            if (_userToDelete != null)
            {
                await DeleteUserAsync(_userToDelete.Id);
                IsConfirmDeleteDialogOpen = false;
                _userToDelete = null;
            }
        }

        private void CancelDelete()
        {
            IsConfirmDeleteDialogOpen = false;
            _userToDelete = null;
        }
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка пользователей...";
                // Загрузка пользователей
                var users = await _dbContext.Users
                    .Include(u => u.Roles)
                    .Include(u => u.UserDepartments)
                        .ThenInclude(ud => ud.Department)
                    .ToListAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    var userViewModel = new UserViewModel2
                    {
                        Id = user.Id,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email ?? "Не указан",
                        IsActive = user.IsActive ?? true,
                        LastLogin = user.LastLogin?.ToString("dd.MM.yyyy HH:mm") ?? "Никогда",
                        Role = user.Roles.FirstOrDefault()?.Name ?? "Не назначена",
                        Department = user.UserDepartments
                            .Select(ud => ud.Department?.Name)
                            .FirstOrDefault() ?? "Не назначен"
                    };

                    Users.Add(userViewModel);

                }
                
                // Загрузка опций для фильтров
                await LoadFilterOptionsAsync();
                _allUsers = new ObservableCollection<UserViewModel2>(Users);

                StatusMessage = $"Загружено пользователей: {Users.Count}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке пользователей: {ex.Message}");
                StatusMessage = "Ошибка при загрузке пользователей";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task LoadFilterOptionsAsync()
        {
            try
            {
                // Загрузка ролей
                var roles = await _dbContext.Roles
                    .Select(r => r.Name)
                    .ToListAsync();

                RoleOptions.Clear();
                RoleOptions.Add("Все");
                RoleOptions.Add("Администратор");
                RoleOptions.Add("Менеджер");
                RoleOptions.Add("Исполнитель");

                foreach (var role in roles.Where(r => !RoleOptions.Contains(r)))
                {
                    RoleOptions.Add(role);
                }

                // Загрузка отделов
                var departments = await _dbContext.Departments
                    .Select(d => d.Name)
                    .ToListAsync();

                DepartmentOptions.Clear();
                DepartmentOptions.Add("Все");
                foreach (var department in departments)
                {
                    DepartmentOptions.Add(department);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке опций фильтров: {ex.Message}");
            }
        }

        private void FilterUsers()
        {
            if (_allUsers == null || _allUsers.Count == 0)
            {
                Debug.WriteLine("_allUsers is null or empty");
                return;
            }

            Debug.WriteLine($"Filtering with Role: {SelectedRoleFilter}, Department: {SelectedDepartmentFilter}, Search: {SearchText}");

            var filtered = _allUsers.AsEnumerable();

            // Фильтрация по поисковому тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(u =>
                    u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Фильтрация по роли
            if (SelectedRoleFilter != "Все")
            {
                filtered = filtered.Where(u => u.Role == SelectedRoleFilter);
            }

            // Фильтрация по отделу
            if (SelectedDepartmentFilter != "Все")
            {
                filtered = filtered.Where(u => u.Department == SelectedDepartmentFilter);
            }

            Users.Clear();
            foreach (var user in filtered)
            {
                Users.Add(user);
            }
        }

        private void OpenCreateDialog()
        {
            // Очистка полей создания пользователя
            NewUserName = string.Empty;
            NewPassword = string.Empty;
            NewFirstName = string.Empty;
            NewLastName = string.Empty;
            NewEmail = string.Empty;

            IsCreateUserDialogOpen = true;
        }

        private void OpenEditDialog(UserViewModel2 user)
        {
            if (user != null)
            {
                SelectedUser = user;
                IsEditUserDialogOpen = true;
            }
        }

        private void LoadUserForEdit(UserViewModel2 user)
        {
            if (user == null) return; // Добавьте эту проверку

            EditUserName = user.Username;
            EditFirstName = user.FirstName;
            EditLastName = user.LastName;
            EditEmail = user.Email;
            EditIsActive = user.IsActive;

            // Установка выбранной роли и отдела
            EditSelectedRole = Math.Max(0, RoleOptions.IndexOf(user.Role));
            EditSelectedDepartment = Math.Max(0, DepartmentOptions.IndexOf(user.Department));
        }

        private async System.Threading.Tasks.Task CreateUserAsync()
        {
            try
            {
                // Валидация данных
                if (string.IsNullOrWhiteSpace(NewUserName) ||
                    string.IsNullOrWhiteSpace(NewFirstName) ||
                    string.IsNullOrWhiteSpace(NewLastName) ||
                    string.IsNullOrWhiteSpace(NewPassword))
                {
                    StatusMessage = "Заполните все обязательные поля";
                    return;
                }

                // Проверка уникальности имени пользователя
                var existingUser = await _dbContext.Users
                    .AnyAsync(u => u.Username == NewUserName);

                if (existingUser)
                {
                    StatusMessage = "Пользователь с таким именем уже существует";
                    return;
                }

                // Создание нового пользователя
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = NewUserName,
                    Password = NewPassword, // В реальном приложении пароль нужно хешировать
                    FirstName = NewFirstName,
                    LastName = NewLastName,
                    Email = NewEmail,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                StatusMessage = "Пользователь успешно создан";
                IsCreateUserDialogOpen = false;

                // Обновление списка пользователей
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при создании пользователя: {ex.Message}");
                StatusMessage = "Ошибка при создании пользователя";
            }
        }

        private async System.Threading.Tasks.Task SaveUserChangesAsync()
        {
            try
            {
                if (SelectedUser == null) return;

                var user = await _dbContext.Users
                    .Include(u => u.Roles)
                    .Include(u => u.UserDepartments)
                    .FirstOrDefaultAsync(u => u.Id == SelectedUser.Id);

                if (user == null)
                {
                    StatusMessage = "Пользователь не найден";
                    return;
                }

                // Обновление данных пользователя
                user.Username = EditUserName;
                user.FirstName = EditFirstName;
                user.LastName = EditLastName;
                user.Email = EditEmail;
                user.IsActive = EditIsActive;
                user.UpdatedAt = DateTime.Now;

                // Обновление роли
                if (EditSelectedRole > 0 && EditSelectedRole < RoleOptions.Count)
                {
                    var roleName = RoleOptions[EditSelectedRole];
                    var role = await _dbContext.Roles
                        .FirstOrDefaultAsync(r => r.Name == roleName);

                    if (role != null)
                    {
                        user.Roles.Clear();
                        user.Roles.Add(role);
                    }
                }

                // Обновление отдела
                if (EditSelectedDepartment > 0 && EditSelectedDepartment < DepartmentOptions.Count)
                {
                    var departmentName = DepartmentOptions[EditSelectedDepartment];
                    var department = await _dbContext.Departments
                        .FirstOrDefaultAsync(d => d.Name == departmentName);

                    if (department != null)
                    {
                        user.UserDepartments.Clear();
                        user.UserDepartments.Add(new UserDepartment
                        {
                            UserId = user.Id,
                            DepartmentId = department.Id,
                            IsHead = false
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();

                StatusMessage = "Данные пользователя успешно обновлены";
                IsEditUserDialogOpen = false;

                // Обновление списка пользователей
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении пользователя: {ex.Message}");
                StatusMessage = "Ошибка при обновлении пользователя";
            }
        }

        private async System.Threading.Tasks.Task DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    StatusMessage = "Пользователь не найден";
                    return;
                }

                // Проверка, является ли пользователь текущим
                if (user.Id == _mainWindowViewModel.CurrentUserId)
                {
                    StatusMessage = "Нельзя удалить текущего пользователя";
                    return;
                }

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();

                StatusMessage = "Пользователь успешно удален";

                // Обновление списка пользователей
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при удалении пользователя: {ex.Message}");
                StatusMessage = "Ошибка при удалении пользователя";
            }
        }

        private void CancelEdit()
        {
            IsEditUserDialogOpen = false;
            IsCreateUserDialogOpen = false;
            SelectedUser = null;
        }
    }

    // Вспомогательный класс для представления пользователя в списке
    public class UserViewModel2 : ViewModelBase
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public string LastLogin { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
    }
}
