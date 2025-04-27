// Services/UserCredentialService.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace ManufactPlanner.Services
{
    public class UserCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserCredentialService
    {
        private const string CredentialsFileName = "user.dat";
        private readonly string _filePath;
        // Фиксированный размер ключа для AES (32 байта = 256 бит)
        private readonly byte[] _key;

        public UserCredentialService()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ManufactPlanner");

            // Создаем директорию, если она не существует
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _filePath = Path.Combine(appDataPath, CredentialsFileName);

            // Создаем ключ подходящего размера (32 байта для AES-256)
            _key = CreateKey("ManufactPlanner_EncryptionKey_2025!");
        }

        // Метод для создания ключа нужного размера
        private byte[] CreateKey(string password)
        {
            // Используем SHA256 для получения ключа нужной длины из пароля
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task SaveCredentialsAsync(string username, string password)
        {
            var credentials = new UserCredentials
            {
                Username = username,
                Password = password
            };

            var json = JsonSerializer.Serialize(credentials);
            var encryptedData = Encrypt(json);

            await File.WriteAllBytesAsync(_filePath, encryptedData);
        }

        public async Task<UserCredentials> LoadCredentialsAsync()
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            try
            {
                var encryptedData = await File.ReadAllBytesAsync(_filePath);
                var json = Decrypt(encryptedData);
                return JsonSerializer.Deserialize<UserCredentials>(json);
            }
            catch
            {
                // Если произошла ошибка при чтении или расшифровке, возвращаем null
                return null;
            }
        }

        public void ClearCredentials()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        private byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.GenerateIV(); // Генерируем случайный вектор инициализации

                using (MemoryStream ms = new MemoryStream())
                {
                    // Сначала записываем IV в поток
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] textBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(textBytes, 0, textBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        private string Decrypt(byte[] cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;

                // Размер блока IV всегда равен размеру блока алгоритма (16 байт для AES)
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(
                        new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length),
                        aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
