using MetaData.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Data
{
    public class CameraData
    {
        private static CameraData? instance;
        public CameraData()
        {

        }

        public static CameraData getInstance()
        {
            if (instance == null)
            {
                instance = new CameraData();
            }
            return instance;
        }

        public List<CameraBusiness> CameraBusinesses = new();
    }
}
