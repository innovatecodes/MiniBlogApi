using System.Security.Cryptography;
using System.Text;

namespace MiniBlog.Api.Extensions
{
    public static class PasswordHasherExtensions
    {
        public static string HashPassword(this string plainTextPassword)
        {
            // Converte a senha de string para um array de bytes, pois o algoritmo SHA-256 trabalha com bytes
            var passwordBytes = Encoding.UTF8.GetBytes(plainTextPassword);

            // Aplica o algoritmo SHA-256 aos bytes da senha e gera um hash (array de bytes)
            var hashedBytes = SHA256.HashData(passwordBytes);

            // Cria um StringBuilder para armazenar a versão em string do hash
            var hashedPassword = new StringBuilder();

            // Percorre cada byte do hash e converte para uma string hexadecimal
            foreach (var hashedByte in hashedBytes)
                hashedPassword.Append(hashedByte.ToString("X2")); // "X2" converte para hexadecimal com 2 caracteres

            // Retorna a string hexadecimal final representando a senha hasheada
            return hashedPassword.ToString();
        }
    }
}
