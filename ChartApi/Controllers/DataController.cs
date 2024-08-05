using ChartApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ChartApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _loginName;
        private readonly string _password;

        public DataController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _loginName = configuration["LoginInfo:LoginName"];
            _password = configuration["LoginInfo:LoginPassword"];
        }

        private bool ValidateLogin(string loginName, string password)
        {
            return _loginName == loginName && _password == password;
        }


        // Saklı prosedürü çağıran metot
        [HttpPost("GetDataBySp")]
        public async Task<IActionResult> GetDataBySp([FromBody] RequestModel requestModel)
        {
            if (!ValidateLogin(requestModel.LoginName, requestModel.Password))
            {
                return Unauthorized("Bağlantı hatası: Geçersiz kullanıcı adı veya şifre.");
            }

            var result = new List<Dictionary<string, object>>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_GetDataByServerName", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Servername", requestModel.ServerName);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }
                                result.Add(row);
                            }
                        }
                    }
                }
                return Ok(result);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, $"Veritabanı hatası: {sqlEx.Message}");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Bilinmeyen bir hata oluştu: {ex.Message}");
            }
        }

        // Fonksiyonu çağıran metot
        [HttpPost("GetDataByFunction")]
        public async Task<IActionResult> GetDataByFunction([FromBody] RequestModel requestModel)
        {
            if (!ValidateLogin(requestModel.LoginName, requestModel.Password))
            {
                return Unauthorized("Bağlantı hatası: Geçersiz kullanıcı adı veya şifre.");
            }

            var result = new List<Dictionary<string, object>>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Table-valued function'ı çağıran SQL sorgusu
                    var query = "SELECT * FROM dbo.GetDataByServerName(@Servername)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Servername", requestModel.ServerName);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }
                                result.Add(row);
                            }
                        }
                    }
                }
                return Ok(result);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, $"Veritabanı hatası: {sqlEx.Message}");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Bilinmeyen bir hata oluştu: {ex.Message}");
            }
        }

        // Viewi çağıran metot
        [HttpPost("GetDataByView")]
        public async Task<IActionResult> GetDataByView([FromBody] RequestModel requestModel)
        {
            if (!ValidateLogin(requestModel.LoginName, requestModel.Password))
            {
                return Unauthorized("Bağlantı hatası: Geçersiz kullanıcı adı veya şifre.");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Stored Procedure çalıştırma ve DatasetId alma
                    var datasetId = await GetDatasetIdAsync(connection, requestModel.ServerName);
                    if (datasetId == null)
                    {
                        return NotFound("Veri seti bulunamadı.");
                    }

                    // View'den veri çekme
                    var result = await GetDataFromViewAsync(connection, datasetId.Value);
                    return Ok(result);
                }
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, $"Veritabanı hatası: {sqlEx.Message}");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Bilinmeyen bir hata oluştu: {ex.Message}");
            }
        }

        private async Task<int?> GetDatasetIdAsync(SqlConnection connection, string serverName)
        {
            using (var command = new SqlCommand("sp_GetDatasetId", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Servername", serverName);

                var result = await command.ExecuteScalarAsync();
                return result != null ? (int?)Convert.ToInt32(result) : null;
            }
        }

        private async Task<List<Dictionary<string, object>>> GetDataFromViewAsync(SqlConnection connection, int datasetId)
        {
            var result = new List<Dictionary<string, object>>();
            string query = "SELECT * FROM dbo.ViewDataByServerName WHERE DatasetId = @DatasetId";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DatasetId", datasetId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }
            }

            return result;
        }


        [HttpPost("Auth")]
        public IActionResult Auth([FromBody] AuthModel authModel)
        {
            if (!ValidateLogin(authModel.LoginName, authModel.Password))
            {
                return Ok("Geçersiz kullanıcı adı veya şifre.");
            }

            return Ok("Başarılı bağlantı.");
        }


    }

}
