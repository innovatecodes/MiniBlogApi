using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.IdentityModel.Protocols.Configuration;
using MiniBlog.Api.Data;
using System;
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
                var sql = "SELECT id, displayName AS name, email, createdAt, lastModified FROM users"; 

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
                var query = "SELECT id, displayName AS name, email, createdAt, lastModified FROM users WHERE id = @id"; 

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
            app.MapPost("/api/v1/users", async (ConfigureDbConnection dbConnection, User user) =>
            {
                var command = @"INSERT INTO users (displayName, email, pwd, reEnterPwd)
                                VALUES(@displayName, @email, @pwd, @reEnterPwd)";
                var query = "SELECT CAST(SCOPE_IDENTITY() AS INT)";

                using (var connection = await dbConnection())
                {
                    var transaction = connection.BeginTransaction();

                    try
                    {
                        var emailExists = await EmailExists(user, connection, transaction);

                        if (emailExists) return Results.Conflict("E-mail já cadastrado!");

                        var userId = await connection.ExecuteScalarAsync<int>(
                            command + query,
                             new
                             {
                                 displayName = user.DisplayName,
                                 email = user.Email,
                                 pwd = user.Pwd,
                                 reEnterPwd = user.ReEnterPwd
                             },
                            transaction: transaction);

                        var response = await connection.QuerySingleOrDefaultAsync(
                            @"SELECT id, displayName AS name, email, createdAt, lastModified
                              FROM users WHERE id = @id",
                            new
                            {
                                id = userId
                            },
                            transaction: transaction);

                        transaction.Commit();

                        return Results.Created($"/api/v1/users/{userId}", new { response?.id } /*response*/); 
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
            app.MapPut("/api/v1/users/{id}", async (ConfigureDbConnection dbConnection, int id, User user) =>
            {
                var command = @"UPDATE users 
                                SET displayName = @displayName, 
                                email = @email, 
                                pwd = @pwd, 
                                reEnterPwd = @reEnterPwd, 
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
                                id = id,
                                displayName = user.DisplayName,
                                email = user.Email,
                                pwd = user.Pwd,
                                reEnterPwd = user.ReEnterPwd,
                                lastModified = DateTime.UtcNow // user.Update().LastModified 
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

            #region DELETE /api/v1/users/{id}
            app.MapDelete("/api/v1/users/{id}", async (ConfigureDbConnection dbConnection, int id) =>
            {
                var command = "DELETE FROM users WHERE id = @id";

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

        private static async Task<bool> EmailExists(User user, IDbConnection connection, IDbTransaction transaction, int? id = null) 
        {
            var query = "SELECT email FROM users WHERE id = @id AND email = @email";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { id = id, email = user.Email }, transaction: transaction) != null;
        }
    }
}
