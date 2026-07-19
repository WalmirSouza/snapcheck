using System.Security.Cryptography;
using System.Text;

namespace SnapCheck.Data.Security;

public static class AcessoCrypto
{
    private const int Iteracoes = 100_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public static (string Salt, string Hash) GerarHashSenha(string senha)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltLength);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            saltBytes,
            Iteracoes,
            HashAlgorithmName.SHA256,
            HashLength);

        return (Convert.ToBase64String(saltBytes), Convert.ToBase64String(hashBytes));
    }

    public static bool VerificarSenha(string senha, string saltBase64, string hashBase64)
    {
        var saltBytes = Convert.FromBase64String(saltBase64);
        var hashBytes = Convert.FromBase64String(hashBase64);
        var tentativa = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            saltBytes,
            Iteracoes,
            HashAlgorithmName.SHA256,
            hashBytes.Length);

        return CryptographicOperations.FixedTimeEquals(tentativa, hashBytes);
    }

    public static (string Token, string Hash) GerarTokenSeguro()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return (token, HashToken(token));
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}