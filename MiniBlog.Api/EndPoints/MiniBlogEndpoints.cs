using Dapper;
using MiniBlog.Api.Data;
using MiniBlog.Api.Extensions;
using System.Data;
using static MiniBlog.Api.Data.MiniBlogContext;

namespace MiniBlog.Api.EndPoints
{
    public static class MiniBlogEndpoints
    {
        public static void MapMiniBlogEndpoints(this WebApplication app)
        {
            #region GET /api/v1/users
            app.MapGet("/api/v1/users", async (ConfigureDbConnection dbConnection) =>
            {
                var sql = "SELECT id, displayName AS name, email, createdAt, lastModified FROM UserAccounts"; 

                using (var connection = await dbConnection())
                {
                    try
                    {
                        var users = await connection.QueryAsync(sql);

                        if (users is null || !users.Any())
                            return Results.NotFound("Não existem usuários cadastrados!");

                        var response = users.Select(user => new 
                        {
                            user.id,
                            user.name,
                            user.email,
                            user.createdAt,
                            user.lastModified
                        }).ToList(); 

                        return Results.Ok(response);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                }
            }).WithName("GetUsers"); // Define o nome da rota como "GetUsers"
            #endregion

            #region GET /api/v1/users/{id}
            app.MapGet("/api/v1/users/{id}", async (ConfigureDbConnection dbConnection, int id) =>
            {
                var query = "SELECT id, displayName AS name, email, createdAt, lastModified FROM UserAccounts WHERE id = @id"; 

                using (var connection = await dbConnection())
                {
                    try
                    {
                        var user = await connection.QueryFirstOrDefaultAsync(query, new { id = id });

                        if (user is null) return Results.NotFound("Nenhum usuário encontrado!");

                        var response = new
                        {
                            user.id,
                            user.name,
                            user.email,
                            user.createdAt,
                            user.lastModified
                        };

                        return Results.Ok(response);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                }
            }).WithName("GetUserById"); // Define o nome da rota como "GetUserById"
            #endregion

            #region POST /api/v1/users
            app.MapPost("/api/v1/users", async (ConfigureDbConnection dbConnection, UserAccount user) =>
            {
                var command = @"INSERT INTO UserAccounts (displayName, email, pwd)
                                VALUES(@displayName, @email, @pwd)";
                var query = "SELECT CAST(SCOPE_IDENTITY() AS INT)";

                using (var connection = await dbConnection())
                {
                    var transaction = connection.BeginTransaction();

                    try
                    {
                        var emailExists = await EmailExists(user, connection, transaction);
                        var result = PasswordMatches(user);

                        if (emailExists) return Results.Conflict("E-mail já cadastrado!");

                        if (result is not null) return result;

                        var userId = await connection.ExecuteScalarAsync<int>(
                            command + query,
                             new
                             {
                                 displayName = user.DisplayName,
                                 email = user.Email,
                                 pwd = user.Pwd.HashPassword()
                             },
                            transaction: transaction);

                        var response = await connection.QuerySingleOrDefaultAsync(
                            @"SELECT id, displayName AS name, email, createdAt, lastModified
                              FROM UserAccounts WHERE id = @id",
                            new
                            {
                                id = userId
                            },
                            transaction: transaction);

                        transaction.Commit();

                        return Results.Created(string.Empty /*$"/api/v1/users/{userId}"*/, new { response?.id } /*response*/);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Results.Problem(ex.Message);
                    }
                }
            }).WithName("CreateUser"); // Define o nome da rota como "CreateUser"
            #endregion

            #region PUT /api/v1/users/{id}
            app.MapPut("/api/v1/users/{id}", async (ConfigureDbConnection dbConnection, int id, UserAccount user) =>
            {
                var command = @"UPDATE UserAccounts 
                                SET displayName = @displayName, 
                                email = @email, 
                                lastModified = @lastModified 
                                WHERE id = @id";

                using (var connection = await dbConnection())
                {
                    var transaction = connection.BeginTransaction();

                    try
                    {
                        var emailExists = await EmailExists(user, connection, transaction, id);

                        if (emailExists) return Results.Conflict("Insira um e-mail diferente!");
                       
                        var rowsAffected = await connection.ExecuteAsync(command,
                            new
                            {
                                id,
                                displayName = user.DisplayName,
                                email = user.Email,
                                lastModified = DateTime.UtcNow 
                            }, 
                            transaction: transaction);

                        if (rowsAffected == 0) return Results.NotFound("Usuário não encontrado!");

                        transaction.Commit();
                        return Results.NoContent();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Results.Problem(ex.Message);
                    }
                }
            }).WithName("UpdateUser"); // Define o nome da rota como "UpdateUser"
            #endregion

            #region PATCH /api/v1/users/{id}
            app.MapPatch("/api/v1/users/{id}", async (ConfigureDbConnection dbConnection, int id, UserAccount user) =>
            {
                var command = @"UPDATE UserAccounts 
                                SET pwd = @pwd,  
                                lastModified = @lastModified 
                                WHERE id = @id";

                using (var connection = await dbConnection())
                {
                    var transaction = connection.BeginTransaction();

                    try
                    {
                        var passwordExists = await PasswordExists(user, connection, transaction, id);
                        var result = PasswordMatches(user);

                        if(result is not null) return result;

                        if (passwordExists) return Results.BadRequest("A nova senha não pode ser igual à anterior!");

                        var rowsAffected = await connection.ExecuteAsync(command,
                            new
                            {
                                id,
                                pwd = user.Pwd.HashPassword(),
                                lastModified = user.Update().LastModified
                            },
                            transaction: transaction);
                        if (rowsAffected == 0) return Results.NotFound("Usuário não encontrado!");

                        transaction.Commit();

                        return Results.NoContent();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Results.Problem(ex.Message);
                    }
                }
            }).WithName("UpdateUserPassword"); // Define o nome da rota como "UpdateUserPassword"
            #endregion

            #region DELETE /api/v1/users/{id}
            app.MapDelete("/api/v1/users/{id}", async (ConfigureDbConnection dbConnection, int id) =>
            {
                var command = "DELETE FROM UserAccounts WHERE id = @id";

                using (var connection = await dbConnection())
                {
                    var transaction = connection.BeginTransaction();

                    try
                    {
                        var rowsAffected = await connection.ExecuteAsync(command, new { id }, transaction: transaction);

                        if (rowsAffected == 0) return Results.NotFound("Nenhum usuário encontrado!");

                        transaction.Commit();

                        return Results.NoContent();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Results.Problem(ex.Message);
                    }
                }
            }).WithName("DeleteUser"); // Define o nome da rota como "DeleteUser"
            #endregion
        }

        #region Private Methods
        private static IResult? PasswordMatches(UserAccount user) => user.Pwd != user.ReEnterPwd ? Results.BadRequest("As senhas não coincidem!") : null;
    
        private static async Task<bool> EmailExists(UserAccount user, IDbConnection connection, IDbTransaction transaction, int? id = null) 
        {
            var query = id is not null ? "SELECT email FROM UserAccounts WHERE id = @id AND email = @email" : "SELECT email FROM UserAccounts WHERE email = @email";

            if (id is not null) return await connection.QueryFirstOrDefaultAsync<string>(query, new { id, email = user.Email }, transaction: transaction) != null;
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { email = user.Email }, transaction: transaction) != null;
        }

        private static async Task<bool> PasswordExists(UserAccount user, IDbConnection connection, IDbTransaction transaction, int? id = null)
        {
            var query = "SELECT pwd FROM UserAccounts WHERE id = @id AND pwd = @pwd";
            return await connection.QuerySingleOrDefaultAsync(query, new { id, pwd = user.Pwd.HashPassword() }, transaction: transaction) != null; 
        }
        #endregion
    }
}
