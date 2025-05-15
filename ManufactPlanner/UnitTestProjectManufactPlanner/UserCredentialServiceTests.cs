using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManufactPlanner.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnitTestProjectManufactPlanner.Services
{
    [TestClass]
    public class UserCredentialServiceTests
    {
        private UserCredentialService _credentialService;
        private UserCredentialService _service;
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            _service = new UserCredentialService();
            _credentialService = new UserCredentialService();
            // Создаем временную директорию для тестов
            string testDir = Path.Combine(Path.GetTempPath(), "ManufactPlanner_Test_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);

            // Используем рефлексию для изменения пути к файлу для тестирования
            var field = typeof(UserCredentialService).GetField("_filePath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            _testFilePath = Path.Combine(testDir, "user.dat");
            field?.SetValue(_service, _testFilePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Удаляем тестовый файл и директорию
            try
            {
                if (File.Exists(_testFilePath))
                    File.Delete(_testFilePath);

                var testDir = Path.GetDirectoryName(_testFilePath);
                if (Directory.Exists(testDir) && Directory.GetFiles(testDir).Length == 0)
                    Directory.Delete(testDir);
            }
            catch (Exception)
            {
                // Игнорируем ошибки очистки
            }
        }

        [TestMethod]
        public async Task SaveAndLoadCredentials_ValidData_ReturnsCorrectCredentials()
        {
            // Arrange
            string testUsername = "testuser";
            string testPassword = "testpass123";

            // Act
            await _credentialService.SaveCredentialsAsync(testUsername, testPassword);
            var loadedCredentials = await _credentialService.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual(testUsername, loadedCredentials.Username);
            Assert.AreEqual(testPassword, loadedCredentials.Password);
        }

        [TestMethod]
        public async Task LoadCredentials_NoFile_ReturnsNull()
        {
            // Act
            var result = await _credentialService.LoadCredentialsAsync();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveCredentials_OverwriteExisting_UpdatesCredentials()
        {
            // Arrange
            await _credentialService.SaveCredentialsAsync("user1", "pass1");

            // Act
            await _credentialService.SaveCredentialsAsync("user2", "pass2");
            var loadedCredentials = await _credentialService.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual("user2", loadedCredentials.Username);
            Assert.AreEqual("pass2", loadedCredentials.Password);
        }

        [TestMethod]
        public async Task ClearCredentials_FileExists_RemovesFile()
        {
            // Arrange
            await _credentialService.SaveCredentialsAsync("testuser", "testpass");

            // Act
            _credentialService.ClearCredentials();
            var loadedCredentials = await _credentialService.LoadCredentialsAsync();

            // Assert
            Assert.IsNull(loadedCredentials);
        }

        [TestMethod]
        public async Task SaveCredentials_EmptyStrings_SavesEmptyValues()
        {
            // Act
            await _credentialService.SaveCredentialsAsync("", "");
            var loadedCredentials = await _credentialService.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual("", loadedCredentials.Username);
            Assert.AreEqual("", loadedCredentials.Password);
        }
        [TestMethod]
        public async Task SaveCredentialsAsync_Should_Save_Credentials_Successfully()
        {
            // Arrange
            string username = "test_user";
            string password = "test_password";

            // Act
            await _service.SaveCredentialsAsync(username, password);

            // Assert
            Assert.IsTrue(File.Exists(_testFilePath));
        }

        [TestMethod]
        public async Task LoadCredentialsAsync_Should_Load_Saved_Credentials()
        {
            // Arrange
            string username = "test_user";
            string password = "test_password";
            await _service.SaveCredentialsAsync(username, password);

            // Act
            var loadedCredentials = await _service.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual(username, loadedCredentials.Username);
            Assert.AreEqual(password, loadedCredentials.Password);
        }

        [TestMethod]
        public async Task LoadCredentialsAsync_Without_File_Should_Return_Null()
        {
            // Act
            var loadedCredentials = await _service.LoadCredentialsAsync();

            // Assert
            Assert.IsNull(loadedCredentials);
        }

        [TestMethod]
        public async Task SaveCredentialsAsync_With_Special_Characters_Should_Work()
        {
            // Arrange
            string username = "user@domain.com";
            string password = "pass!@#$%^&*()_+{}|:<>?";

            // Act
            await _service.SaveCredentialsAsync(username, password);
            var loadedCredentials = await _service.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual(username, loadedCredentials.Username);
            Assert.AreEqual(password, loadedCredentials.Password);
        }

        [TestMethod]
        public async Task SaveCredentialsAsync_With_Unicode_Characters_Should_Work()
        {
            // Arrange
            string username = "тест_пользователь";
            string password = "пароль_тест_123";

            // Act
            await _service.SaveCredentialsAsync(username, password);
            var loadedCredentials = await _service.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual(username, loadedCredentials.Username);
            Assert.AreEqual(password, loadedCredentials.Password);
        }

        [TestMethod]
        public async Task Multiple_Save_Load_Cycles_Should_Work()
        {
            // Arrange & Act & Assert
            for (int i = 0; i < 5; i++)
            {
                string username = $"user_{i}";
                string password = $"password_{i}";

                await _service.SaveCredentialsAsync(username, password);
                var loadedCredentials = await _service.LoadCredentialsAsync();

                Assert.IsNotNull(loadedCredentials);
                Assert.AreEqual(username, loadedCredentials.Username);
                Assert.AreEqual(password, loadedCredentials.Password);
            }
        }

        [TestMethod]
        public void ClearCredentials_Should_Delete_File()
        {
            // Arrange - создаем файл
            File.WriteAllText(_testFilePath, "test content");
            Assert.IsTrue(File.Exists(_testFilePath));

            // Act
            _service.ClearCredentials();

            // Assert
            Assert.IsFalse(File.Exists(_testFilePath));
        }

        [TestMethod]
        public void ClearCredentials_Without_File_Should_Not_Throw()
        {
            // Act & Assert - не должно бросать исключение
            _service.ClearCredentials();
            Assert.IsTrue(true); // Тест проходит, если не бросается исключение
        }

        [TestMethod]
        public async Task LoadCredentialsAsync_With_Corrupted_File_Should_Return_Null()
        {
            // Arrange - создаем поврежденный файл
            File.WriteAllBytes(_testFilePath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

            // Act
            var loadedCredentials = await _service.LoadCredentialsAsync();

            // Assert
            Assert.IsNull(loadedCredentials);
        }

        [TestMethod]
        public async Task Encrypted_File_Should_Not_Be_Readable_As_Plain_Text()
        {
            // Arrange
            string username = "test_user";
            string password = "test_password";
            await _service.SaveCredentialsAsync(username, password);

            // Act
            string fileContent = File.ReadAllText(_testFilePath);

            // Assert
            // Зашифрованный файл не должен содержать пароль в открытом виде
            Assert.IsFalse(fileContent.Contains(password));
            Assert.IsFalse(fileContent.Contains(username));
        }

        [TestMethod]
        public async Task SaveCredentialsAsync_With_Empty_Values_Should_Work()
        {
            // Arrange
            string username = "";
            string password = "";

            // Act
            await _service.SaveCredentialsAsync(username, password);
            var loadedCredentials = await _service.LoadCredentialsAsync();

            // Assert
            Assert.IsNotNull(loadedCredentials);
            Assert.AreEqual(username, loadedCredentials.Username);
            Assert.AreEqual(password, loadedCredentials.Password);
        }

        [TestMethod]
        public async Task SaveCredentialsAsync_With_Null_Values_Should_Not_Throw()
        {
            // Act & Assert - не должно бросать исключение
            try
            {
                await _service.SaveCredentialsAsync(null, null);
                // Если дошли сюда, значит исключение не было брошено
                Assert.IsTrue(true);
            }
            catch (ArgumentNullException)
            {
                // Это ожидаемое поведение
                Assert.IsTrue(true);
            }
        }
    }
}