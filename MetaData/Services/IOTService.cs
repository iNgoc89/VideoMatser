﻿using Dapper;
using MetaData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace MetaData.Services
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
        public void P_ConcatVideoCamera_Update(int id, string videoUri, int status)
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
                        VideoUri = videoUri,
                        Status = status
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
        public void P_ConcatVideoCamera_UpdateStatus(int id, int status)
        {
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"cmrs.P_ConcatVideoCamera_UpdateStatus";
                try
                {
                    var pars = new DynamicParameters();
                    pars.AddDynamicParams(new
                    {
                        Id = id,
                        Status = status
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

        public List<CameraModel> GetCameras()
        {
            List<CameraModel> cameras = new List<CameraModel>();
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"select * from cmrs.GetCameraData";
                try
                {
                    cameras = connection.Query<CameraModel>(sql: sql,
                     commandType: CommandType.Text).ToList();

                    return cameras;
                }
                catch (Exception ex)
                {
                    var mess = ex.Message;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) connection.Close();
                }
            }

            return cameras;
        }
    }
}
