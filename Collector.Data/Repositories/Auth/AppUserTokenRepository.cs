using Dapper;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Data.Repositories.Auth
{
    public class AppUserTokenRepository : IAppUserTokenRepository, IDisposable
    {
        readonly IDbConnection _dbConnection;
        public AppUserTokenRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task Add(AppUserTokens token)
        {
            string query = "INSERT INTO AppUserTokens (Token, AppUserId, IsSpecialUser, SpecialUserName, Expiry, Created, IPAddress) VALUES (@Token, @AppUserId, @IsSpecialUser, @SpecialUserName, @Expiry, getutcdate(), @IPAddress)";
            await _dbConnection.ExecuteAsync(query, token);
        }

        public void Dispose()
        {
            _dbConnection.Dispose();
        }

        public async Task ExpireAllTokens(Guid siteUserId)
        {
            string query = "UPDATE AppUserTokens SET Expiry =  GETUTCDATE() WHERE AppUserId = @siteUserId";
            await _dbConnection.ExecuteAsync(query, new { siteUserId });
        }

        public async Task ExpireToken(string token)
        {
            string query = "UPDATE AppUserTokens SET Expiry =  GETUTCDATE() WHERE token = @token";
            await _dbConnection.ExecuteAsync(query, new { token });
        }

        public async Task ExtendRefreshToken(AppUserTokens token)
        {
            string query = "UPDATE AppUserTokens SET Expiry = @Expiry WHERE Token = @Token";
            await _dbConnection.ExecuteAsync(query, token);
        }

        public async Task<AppUserTokens> FindByToken(string token)
        {
            string query = "SELECT * FROM AppUserTokens WHERE Token = @token";
            return await _dbConnection.QueryFirstOrDefaultAsync<AppUserTokens>(query, new { token });
        }

        public async Task<AppUserTokens> FindByTokenIP(string token, string userIP)
        {
            string query = "SELECT * FROM AppUserTokens WHERE Token = @token"; // AND IPAddress = @userIP";
            return await _dbConnection.QueryFirstOrDefaultAsync<AppUserTokens>(query, new { token, userIP });
        }

        public async Task<bool> IsTokenUnique(string token)
        {
            string query = "SELECT * FROM AppUserTokens WHERE Token = @token";
            var result = await _dbConnection.QueryFirstOrDefaultAsync<AppUserTokens>(query, new { token });
            if (result == null) return true;
            else return false;
        }

        public async Task<bool> IsTokenValid(string token)
        {
            string query = "SELECT * FROM AppUserTokens WHERE Token = @token AND Expiry > GETUTCDATE()";
            var result = await _dbConnection.QueryFirstOrDefaultAsync<AppUserTokens>(query, new { token });
            if (result != null) return true;
            return false;
        }
    }
}
