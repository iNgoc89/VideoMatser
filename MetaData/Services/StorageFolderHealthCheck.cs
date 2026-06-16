using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data;

namespace MetaData.Services
{
    public class StorageFolderHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public StorageFolderHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var connectionString = _configuration.GetConnectionString("XMHTConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return HealthCheckResult.Unhealthy("Missing XMHTConnection connection string.");
            }

            var folderIds = new Dictionary<string, long?>
            {
                ["VideoDelete"] = ReadFolderId("VideoDelete"),
                ["VideoSave"] = ReadFolderId("VideoSave"),
                ["CmdDelete"] = ReadFolderId("CmdDelete"),
                ["ImageSave"] = ReadFolderId("ImageSave"),
                ["ImageDelete"] = ReadFolderId("ImageDelete")
            };

            var invalidConfigs = folderIds
                .Where(x => !x.Value.HasValue || x.Value.Value <= 0)
                .Select(x => x.Key)
                .ToArray();

            if (invalidConfigs.Length > 0)
            {
                return HealthCheckResult.Unhealthy($"Invalid ThuMucNghiepVu config: {string.Join(", ", invalidConfigs)}.");
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var missingFolders = new List<string>();
                foreach (var folder in folderIds)
                {
                    var path = await ReadFolderPath(connection, folder.Value!.Value);
                    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    {
                        missingFolders.Add($"{folder.Key}({folder.Value})={path ?? "not found"}");
                    }
                }

                if (missingFolders.Count > 0)
                {
                    return HealthCheckResult.Unhealthy($"Storage folders are missing or inaccessible: {string.Join("; ", missingFolders)}.");
                }

                return HealthCheckResult.Healthy("Configured storage folders are accessible.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Could not verify configured storage folders.", ex);
            }
        }

        private long? ReadFolderId(string key)
        {
            return long.TryParse(_configuration[$"ThuMucNghiepVu:{key}"], out var id) ? id : null;
        }

        private static async Task<string?> ReadFolderPath(SqlConnection connection, long id)
        {
            var parameters = new DynamicParameters();
            parameters.AddDynamicParams(new
            {
                GID = (Guid?)null,
                ID = id
            });

            var folder = await connection.QueryFirstOrDefaultAsync<StorageFolder>(
                "apps.p_ThuMuc_LayTheoID",
                parameters,
                commandType: CommandType.StoredProcedure);

            return folder?.DuongDan;
        }

        private class StorageFolder
        {
            public string? DuongDan { get; set; }
        }
    }
}
