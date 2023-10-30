using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FFmpegWebAPI.Services
{
    public class IOTService
    {
        public readonly string? _connectionString;
        IDbConnection Connection { get { return new SqlConnection(_connectionString); } }
        public IOTService(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:IOTConnection"];
        }
        public int P_ConcatVideoCamera_Insert(Guid guid, int cameraId, DateTime beginDate, DateTime endDate)
        {
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"cmrs.P_ConcatVideoCamera_Insert";
                try
                {
                    var pars = new DynamicParameters();
                    pars.AddDynamicParams(new
                    {
                        GID = guid,
                        CameraId = cameraId,
                        BeginDate = beginDate,
                        EndDate = endDate
                    });
                    pars.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ret = connection.Query<int>(sql: sql, param: pars,
                     commandType: CommandType.StoredProcedure);
                    int Id = pars.Get<int?>("Id") ?? 0;


                    return Id;
                }
                catch (Exception)
                {
                    //_logger.LogError(ex, $"Lỗi {System.Reflection.MethodInfo.GetCurrentMethod()}");
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) connection.Close();
                }
            }
            return 0;
        }
        public void P_ConcatVideoCamera_Update(int id, string videoUri)
        {
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"cmrs.P_ConcatVideoCamera_Update";
                try
                {
                    var pars = new DynamicParameters();
                    pars.AddDynamicParams(new
                    {
                      Id = id,
                      VideoUri = videoUri
                    });

                    var ret = connection.Query(sql: sql, param: pars,
                     commandType: CommandType.StoredProcedure);

                }
                catch (Exception)
                {
                    //_logger.LogError(ex, $"Lỗi {System.Reflection.MethodInfo.GetCurrentMethod()}");
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) connection.Close();
                }
            }
          
        }
    }
}
