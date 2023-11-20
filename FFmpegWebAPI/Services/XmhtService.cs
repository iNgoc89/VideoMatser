using Dapper;
using FFmpegWebAPI.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace FFmpegWebAPI.Services
{
    public class XmhtService
    {
        public readonly string? _connectionString;
        IDbConnection Connection { get { return new SqlConnection(_connectionString); } }
        public XmhtService(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:XMHTConnection"];
        }
        public long TaoThuMuc(Guid? SessionID, long? ThuMucChaID, string TenThuMuc, ref long? ThuMucID, ref string DuongDan)
        {
            var tmc = P_ThuMuc_LayTheoID(SessionID, ThuMucChaID).Result;

            long ret;
            if (tmc != null)
            {
                // kiểm tra thư mục cha có tồn tại trên hệ thống
                if (Directory.Exists(tmc.DuongDan))
                {
                    DuongDan = Path.Combine(tmc.DuongDan, TenThuMuc);

                    // kiểm tra thư mục cần tạo tồn tại trên hệ thống
                    // không tồn tại tạo mới
                    if (!Directory.Exists(DuongDan))
                    {
                        Directory.CreateDirectory(DuongDan);
                    }

                    // Tạo thư mục trong database, nếu đã tồn tại thì trả về ThuMucID
                    ret = P_ThuMuc_Them1(SessionID, ref ThuMucID, TenThuMuc, tmc.ThuMucID);

                    if (ret != 0)
                    {
                        //Debug.WriteLine("Tạo thư mục thất bại. Mã lỗi: " + ret.ToString());
                    }
                }
                else
                {
                    ret = -1;
                    //Debug.WriteLine("Thư mục cha không tồn tại trên hệ thống. Đường dẫn: " + tmc.DuongDan);
                }
            }
            else
            {
                ret = -1;
                //Debug.WriteLine("Thư mục cha không tồn tại trên cơ sở dữ liệu");
            }

            return ret;
        }

        //public long P_ThuMuc_LayTMNgiepVu(Guid? guid, ref long? id, string? tenNghiepVu)
        //{
        //    using (var connection = Connection)
        //    {
        //        connection.Open();
        //        string sql = $"apps.p_ThuMuc_LayTMNgiepVu";
        //        try
        //        {
        //            var pars = new DynamicParameters();
        //            pars.AddDynamicParams(new
        //            {
        //                GID = guid,
        //                ID = id,
        //                TenNghiepVu = tenNghiepVu
        //            });
        //            pars.Add("ID", dbType: DbType.Int64, direction: ParameterDirection.Output);

        //            var ret = connection.Query<long>(sql: sql, param: pars,
        //             commandType: CommandType.StoredProcedure);
        //            long Id = pars.Get<long?>("ID") ?? 0;


        //            return Id;
        //        }
        //        catch (Exception)
        //        {
        //            //_logger.LogError(ex, $"Lỗi {System.Reflection.MethodInfo.GetCurrentMethod()}");
        //        }
        //        finally
        //        {
        //            if (connection.State == ConnectionState.Open) connection.Close();
        //        }
        //    }
        //    return 0;
        //}
        public async Task<ThuMuc?> P_ThuMuc_LayTheoID(Guid? guid, long? id)
        {
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"apps.p_ThuMuc_LayTheoID";
                try
                {
                    var pars = new DynamicParameters();
                    pars.AddDynamicParams(new
                    {
                        GID = guid,
                        ID = id,
                    });


                    var ret = await connection.QueryAsync<ThuMuc>(sql: sql, param: pars,
                     commandType: CommandType.StoredProcedure);

                    return ret.FirstOrDefault() ?? null;
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
            return null;
        }

        public long P_ThuMuc_Them1(Guid? guid, ref long? id, string tenThuMuc, long? thuMucChaID)
        {
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"apps.p_ThuMuc_Them1";
                try
                {
                    var pars = new DynamicParameters();
                    pars.AddDynamicParams(new
                    {
                        GID = guid,
                        ID = id,
                        TenThuMuc = tenThuMuc,
                        ThuMucChaID = thuMucChaID
                    });
                    pars.Add("ID", dbType: DbType.Int64, direction: ParameterDirection.Output);

                    var ret = connection.Query<long>(sql: sql, param: pars,
                     commandType: CommandType.StoredProcedure);
                    long Id = pars.Get<long?>("ID") ?? 0;


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
    }
}
