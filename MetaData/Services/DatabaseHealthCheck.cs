using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MetaData.Services
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public DatabaseHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var checks = new Dictionary<string, string?>
            {
                ["IOTConnection"] = _configuration.GetConnectionString("IOTConnection"),
                ["XMHTConnection"] = _configuration.GetConnectionString("XMHTConnection")
            };

            foreach (var check in checks)
            {
                if (string.IsNullOrWhiteSpace(check.Value))
                {
                    return HealthCheckResult.Unhealthy($"Missing {check.Key} connection string.");
                }
            }

            try
            {
                foreach (var check in checks)
                {
                    await using var connection = new SqlConnection(check.Value);
                    await connection.OpenAsync(cancellationToken);

                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    command.CommandTimeout = 5;
                    await command.ExecuteScalarAsync(cancellationToken);
                }

                return HealthCheckResult.Healthy("IOT and XMHT databases are reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("A required database is not reachable.", ex);
            }
        }
    }
}
