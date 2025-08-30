using Dapper;
using Collector.Data.Models;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Collector.Data.Repositories.Auth
{
    public class AppUserRepository : IAppUserRepository, IDisposable
    {
        private string _tableName = "[AppUsers]";
        private string _userRoleTableName = "";
        private string _roleTableName = "";

        readonly IDbConnection _dbConnection;
        public AppUserRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;

            var attribute = typeof(AppUser).GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (attribute != null)
                _tableName = attribute.Name;
            attribute = typeof(AppUserRole).GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (attribute != null)
                _userRoleTableName = attribute.Name;
            attribute = typeof(AppRole).GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (attribute != null)
                _roleTableName = attribute.Name;
        }

        public void Dispose()
        {
            _dbConnection?.Dispose();
        }

        #region "Get Users"

        public int GetTotalUsers()
        {
            string query = @$"SELECT COUNT(*) FROM {_tableName} WHERE [Status]=1";
            return _dbConnection.ExecuteScalar<int>(query);
        }

        public async Task<IEnumerable<AppUser>> GetAll()
        {
            string query = @$"SELECT *, (
                SELECT MAX(Created) FROM AppUserTokens WHERE AppUserId = SU.Id
            ) AS LastLogin 
            FROM {_tableName} SU 
            WHERE SU.[Status]=1 
            ORDER BY [Email]";
            return await _dbConnection.QueryAsync<AppUser>(query);
        }

        private static string GetSanitizedSortColumn(string sort)
        {
            // Define valid columns and directions
            var validColumns = new HashSet<string> { "Email", "FullName", "RadioStation", "Created", "AR.Name", "Owner", "LastLogin" };
            var validDirections = new HashSet<string> { "ASC", "DESC" };

            if (string.IsNullOrWhiteSpace(sort))
                return "Email ASC"; // Default sort

            var parts = sort.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return "Email ASC"; // Default sort if format is invalid

            var column = parts[0];
            var direction = parts[1].ToUpper();

            // Validate column and direction
            if (validColumns.Contains(column) && validDirections.Contains(direction))
                return $"{column} {direction}";

            return "Email ASC"; // Default sort if invalid
        }

        public async Task<(IList<FilteredUserResult> items, int totalCount)> GetAllFiltered(string fullName, int role, int radioStationId, string sort, int page = 1, int pageSize = 10)
        {
            string sanitizedSort = GetSanitizedSortColumn(sort);
            int offset = (page - 1) * pageSize;

            // Count query
            string countQuery = @$"
                SELECT COUNT(*)
                FROM {_tableName} SU
                LEFT JOIN (
                    SELECT AppUserId, MIN(AppRoleId) AS RoleId
                    FROM {_userRoleTableName}
                    GROUP BY AppUserId
                ) AUR_MIN ON AUR_MIN.AppUserId = SU.Id
                LEFT JOIN {_roleTableName} AR ON AR.Id = AUR_MIN.RoleId
                WHERE SU.[Status]=1";

            if (!string.IsNullOrEmpty(fullName))
            {
                countQuery += " AND SU.FullName LIKE @FullName";
            }

            if (role > 0)
            {
                countQuery += " AND AR.Id = @Role";
            }

            // Main query with pagination
            string query = @$"
                SELECT 
                SU.Email, 
                SU.FullName, 
                SU.Id, 
                SU.Status, 
                SU.Created, 
                (SELECT MAX(Created) FROM AppUserTokens WHERE AppUserId = SU.Id) AS LastLogin,
                AR.Name AS RoleName,
                AR.Id AS RoleId,
                CASE WHEN AR.Id = 1 THEN 1 ELSE 0 END AS IsAdmin
                FROM {_tableName} SU
                LEFT JOIN (
                    SELECT AppUserId, MIN(AppRoleId) AS RoleId
                    FROM {_userRoleTableName}
                    GROUP BY AppUserId
                ) AUR_MIN ON AUR_MIN.AppUserId = SU.Id
                LEFT JOIN {_roleTableName} AR ON AR.Id = AUR_MIN.RoleId
                WHERE SU.[Status]=1";

            if (!string.IsNullOrEmpty(fullName))
            {
                query += " AND SU.FullName LIKE @FullName";
            }

            if (role > 0)
            {
                query += " AND AR.Id = @Role";
            }

            query += @$"
                GROUP BY 
                    SU.Email, 
                    SU.FullName, 
                    SU.Id, 
                    SU.Status, 
                    SU.Created, 
                    AR.Name,
                    AR.Id
                ORDER BY {sanitizedSort}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";
            try
            {
                var parameters = new { FullName = $"%{fullName}%", Role = role, Offset = offset, PageSize = pageSize };
                
                // Execute count query
                int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countQuery, parameters);
                
                // Execute main query with pagination
                var users = (await _dbConnection.QueryAsync<FilteredUserResult>(query, parameters)).ToList();
                
                return (users, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<AppUser> FindByGuidAsync(Guid userId, bool activeOnly = false)
        {
            string query = @$"SELECT su.*, sr.* FROM {_tableName} su 
                LEFT JOIN {_userRoleTableName} sur on sur.AppUserId = su.Id
                LEFT JOIN {_roleTableName} sr on sr.Id = sur.AppRoleId
                WHERE su.Id = @userId";
            if (activeOnly) query += " AND [Status] = @Status";
            AppUser user = null;
            await _dbConnection.QueryAsync<AppUser, AppRole, AppUser>(query,
                (siteuser, role) =>
                {
                    if (user == null)
                    {
                        user = siteuser;
                        user.UserRoles = new List<AppUserRole>();
                    }
                    if (role != null)
                    {
                        user.UserRoles.Add(new AppUserRole { AppRoleId = role.Id, AppRole = role });
                        user.IsAdmin = user.IsAdmin ? true : role.Name == "admin";
                    }

                    return siteuser;
                },
                new { userId, Status = AppUserStatus.Active },
                splitOn: "Id");
            return user;
        }

        public async Task<AppUser> FindByUserEmailAsync(string emailAddress, bool activeOnly = true)
        {
            string query = @$"SELECT su.*, sr.* FROM {_tableName} su 
                LEFT JOIN {_userRoleTableName} sur on sur.AppUserId = su.Id
                LEFT JOIN {_roleTableName} sr on sr.Id = sur.AppRoleId
                WHERE su.Email = @emailAddress";
            if (activeOnly) query += " AND su.[Status] = @Status";

            AppUser user = null;
            await _dbConnection.QueryAsync<AppUser, AppRole, AppUser>(query,
                (siteuser, role) =>
                {
                    if (user == null)
                    {
                        user = siteuser;
                        user.UserRoles = new List<AppUserRole>();
                    }
                    if (role != null)
                    {
                        user.UserRoles.Add(new AppUserRole { AppRoleId = role.Id, AppRole = role });
                        user.IsAdmin = user.IsAdmin ? true : role.Name == "admin";
                    }
                    return siteuser;
                },
                new { emailAddress, Status = AppUserStatus.Active },
                splitOn: "Id");
            return user;
        }

        public async Task<AppUser> GetRolesByUserEmailAsync(string emailAddress)
        {
            string query = @$"SELECT su.*, sr.* FROM {_tableName} su 
                LEFT JOIN {_userRoleTableName} sur on sur.AppUserId = su.Id
                LEFT JOIN {_roleTableName} sr on sr.Id = sur.AppRoleId
                WHERE su.Email = @emailAddress";

            AppUser user = null;
            await _dbConnection.QueryAsync<AppUser, AppRole, AppUser>(query,
                (siteuser, role) =>
                {
                    if (user == null)
                    {
                        user = siteuser;
                        user.UserRoles = new List<AppUserRole>();
                    }
                    if (role != null)
                        user.UserRoles.Add(new AppUserRole { AppRoleId = role.Id, AppRole = role });
                    return siteuser;
                },
                new { emailAddress, Status = AppUserStatus.Active },
                splitOn: "Id");
            return user;
        }

        #endregion

        #region "Add/Update/Delete User"

        public async Task<AppUser> Add(AppUser user)
        {
            string query = @$"INSERT INTO {_tableName} (Id, Email, EmailConfirmed, FullName, PasswordHash, LockoutEnabled, [Status], PasswordResetTime, PasswordResetHash) 
                OUTPUT INSERTED.* VALUES 
                (newid(), @Email, @EmailConfirmed, @FullName, @PasswordHash, @LockoutEnabled, @Status, @PasswordResetTime, @PasswordResetHash)";
            var newUser = await _dbConnection.QueryFirstOrDefaultAsync<AppUser>(query, user);

            foreach (var role in user.UserRoles.Where(x => x.AppRoleId != 0))
            {
                await _dbConnection.ExecuteAsync($"INSERT INTO {_userRoleTableName} (AppUserId, AppRoleId) VALUES (@AppUserId, @AppRoleId)", new { AppUserId = newUser.Id, role.AppRoleId });
            }
            return newUser;
        }

        public void UpdateInfo(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET 
                [Status] = @Status,
                FullName = @FullName
                WHERE Id = @Id";
            _dbConnection.Execute(query, new { user.Id, user.Status, user.FullName });
        }

        public void UpdateEmail(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET 
                [Email] = @Email
                WHERE Id = @Id";
            _dbConnection.Execute(query, new { user.Id, user.Status, user.FullName });
        }

        public async Task UpdateRole(Guid siteUserId, List<Guid> roleIds)
        {
            //Clear out existing roles
            await _dbConnection.ExecuteAsync($"DELETE FROM {_userRoleTableName} WHERE AppUserId = @AppUserId", new { AppUserId = siteUserId });
            //add back selected role
            foreach (var role in roleIds)
            {
                await _dbConnection.ExecuteAsync($"INSERT INTO {_userRoleTableName} (AppUserId, AppRoleId) VALUES (@AppUserId, @AppRoleId)", new { AppUserId = siteUserId, AppRoleId = role });
            }
        }

        public async Task<bool> UpdateLock(Guid UserId, bool lockUser)
        {
            string query = @$"UPDATE {_tableName} SET [Status] = @Status WHERE Id = @UserId";
            int statusValue = lockUser ? 2 : 1;
            var affected = await _dbConnection.ExecuteAsync(query, new { Status = statusValue, UserId });
            return affected > 0;
        }

        public async Task ActivateAccount(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET EmailConfirmed = 1 WHERE Id = @Id";
            await _dbConnection.QueryAsync(query, new { user.Id });
        }

        public async Task UpdateFailedCount(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET LockoutEnabled = @LockoutEnabled, AccessFailedCount = @AccessFailedCount WHERE Id = @Id";
            await _dbConnection.QueryAsync(query, user);
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            string query = $"UPDATE {_tableName} SET [Status] = @Status WHERE Id = @userId";
            await _dbConnection.ExecuteAsync(query, new { Status = AppUserStatus.Deleted, UserId = userId });
        }

        #endregion

        #region "Update User Password"

        public async Task UpdatePassword(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET PasswordHash = @PasswordHash, LockoutEnabled = 0, AccessFailedCount = 0 WHERE Id = @Id";
            await _dbConnection.QueryAsync(query, user);
        }

        public async Task<AppUser> UpdatePasswordResetHash(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET 
            PasswordResetHash = @PasswordResetHash,
            PasswordResetTime = @PasswordResetTime,
            NewEmail = (CASE WHEN EXISTS(SELECT * FROM {_tableName} WHERE [Email]=@Email AND Id = @Id) THEN NULL ELSE @Email END)";
            query += " WHERE Id = @Id";
            await _dbConnection.QueryAsync(query, user);
            return user;
        }

        public async Task<AppUser> FindByPasswordResetHashAsync(string hashPassword, bool activeOnly = true)
        {
            string query = $"SELECT * FROM {_tableName} WHERE PasswordResetHash = @hashPassword";
            if (activeOnly) query += " AND [Status] = @Status";
            return await _dbConnection.QueryFirstOrDefaultAsync<AppUser>(query, new { hashPassword, Status = AppUserStatus.Active });
        }

        public async Task<AppUser> UpdatePasswordHash(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET PasswordHash = @PasswordHash, PasswordResetTime = @PasswordResetTime, PasswordResetHash = @PasswordResetHash, Email = CASE WHEN NewEmail IS NOT NULL THEN NewEmail ELSE Email END, NewEmail = NULL WHERE Id = @Id";
            await _dbConnection.QueryAsync(query, user);
            return user;
        }

        #endregion

        #region "One Time Login"

        public async Task<AppUser> FindByOneTimeLoginToken(string emailAuthToken, bool activeOnly = true)
        {
            string query = $"SELECT * FROM {_tableName} WHERE OneTimeLoginToken = @emailAuthToken";
            if (activeOnly) query += " AND [Status] = @Status";
            return await _dbConnection.QueryFirstOrDefaultAsync<AppUser>(query, new { emailAuthToken, Status = AppUserStatus.Active });
        }

        public async Task<AppUser> UpdateOneTimeLoginToken(AppUser user)
        {
            string query = @$"UPDATE {_tableName} SET 
            OneTimeLoginToken = @OneTimeLoginToken,
            OneTimeLoginExpiry = @OneTimeLoginExpiry";
            query += " WHERE Id = @Id";
            await _dbConnection.QueryAsync(query, user);
            return user;
        }


        #endregion

    }
}
