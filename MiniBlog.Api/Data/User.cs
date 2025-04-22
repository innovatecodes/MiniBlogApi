using System.Text.Json.Serialization;

namespace MiniBlog.Api.Data
{
    public record User{
        #pragma warning disable 
        public User() { }
        #pragma warning restore

        [JsonConstructor] // Indica qual construtor usar ao desserializar o JSON para um objeto
        public User(string displayName, string email, string pwd, string reEnterPwd) 
        {
            DisplayName = displayName;
            Email = email;
            Pwd = pwd;
            ReEnterPwd = reEnterPwd;
            CreatedAt = DateTime.UtcNow;
        }

        public int Id { get; init; }
        public string DisplayName { get; init; } 
        public string Email { get; init; } 
        public string Pwd { get; init; }
        public string ReEnterPwd { get; init; } 
        public DateTime CreatedAt { get; init; }

        public DateTime? LastModified { get; init; } // public User Update() => this with {  LastModified = DateTime.UtcNow 
    };
}
