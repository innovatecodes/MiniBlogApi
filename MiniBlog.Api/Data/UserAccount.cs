using MiniBlog.Api.Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MiniBlog.Api.Data
{
    public record UserAccount
    {
        #pragma warning disable
        public UserAccount() { }
        #pragma warning restore

        public UserAccount(string pwd, string reEnterPwd)
        {
            Pwd = pwd;
            ReEnterPwd = reEnterPwd;
        }

        [JsonConstructor] // Indica qual construtor usar ao desserializar o JSON para um objeto
        public UserAccount(string displayName, string email, string pwd, string reEnterPwd) : this(pwd, reEnterPwd)
        {
            DisplayName = displayName;
            Email = email;
            //Pwd = pwd;
            //ReEnterPwd = reEnterPwd;
            CreatedAt = DateTime.UtcNow;
        }
   

        public int Id { get; init; }
        public string? DisplayName { get; init; }
        public string? Email { get; init; }
        public string Pwd { get; init; }
        public string ReEnterPwd { get; init; }

        [JsonIgnore] // Ignora a propriedade ao serializar o objeto para JSON
        public DateTime CreatedAt { get; init; }

        [JsonIgnore] // Ignora a propriedade ao serializar o objeto para JSON
        public DateTime? LastModified { get; init; }

        public UserAccount Update() => this with
        {
            LastModified = DateTime.UtcNow  // Atualiza a data de modificação
        };
    }
}