using System.Security.Cryptography;
using System.Text;

namespace ProjectManagerWebAPI.Services;

public interface IPasswordService
{
    string GenerateRandomPassword(int length = 12);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class PasswordService : IPasswordService
{
    private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

    public string GenerateRandomPassword(int length = 12)
    {
        if (length < 8)
            length = 8;

        var password = new StringBuilder();
        using (var rng = new RNGCryptoServiceProvider())
        {
            var allChars = UpperCase + LowerCase + Digits + SpecialChars;
            var charArray = allChars.ToCharArray();
            var buffer = new byte[4];

            // Garantir que tem pelo menos um de cada tipo
            password.Append(GetRandomChar(UpperCase, rng));
            password.Append(GetRandomChar(LowerCase, rng));
            password.Append(GetRandomChar(Digits, rng));
            password.Append(GetRandomChar(SpecialChars, rng));

            // Preencher o resto aleatoriamente
            for (int i = password.Length; i < length; i++)
            {
                rng.GetBytes(buffer);
                int index = BitConverter.ToInt32(buffer, 0) % charArray.Length;
                password.Append(charArray[Math.Abs(index)]);
            }

            // Embaralhar
            var shuffled = ShuffleString(password.ToString(), rng);
            return shuffled;
        }
    }

    public string HashPassword(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    private string GetRandomChar(string chars, RNGCryptoServiceProvider rng)
    {
        var buffer = new byte[4];
        rng.GetBytes(buffer);
        int index = BitConverter.ToInt32(buffer, 0) % chars.Length;
        return chars[Math.Abs(index)].ToString();
    }

    private string ShuffleString(string input, RNGCryptoServiceProvider rng)
    {
        var array = input.ToCharArray();
        var buffer = new byte[4];

        for (int i = array.Length - 1; i > 0; i--)
        {
            rng.GetBytes(buffer);
            int randomIndex = BitConverter.ToInt32(buffer, 0) % (i + 1);
            randomIndex = Math.Abs(randomIndex);

            var temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }

        return new string(array);
    }
}
