using System;
using System.Collections.Generic;
using System.Text;
using CommonLibs;

namespace CommonNetwork
{
    public class PackageManager
    {
        private int m_id = 0;
        public PackageManager(int startId = 0)
        {
            m_id = startId;
        }

        public WebPackage CreateRequestPackage(int action, int uid, int room, byte[] param)
        {
            var package = new WebPackage
            {
                Type = PackageTypeEnum.Request,
                ID = System.Threading.Interlocked.Increment(ref m_id),
                ActionId = action,
                Uid = uid,
                Room = room,
                Params = param,
                ErrorCode = ErrorCodeEnum.Success,
            };
            return package;
        }
        public WebPackage CreateActPackage(int action, int uid, int room, byte[] retData, ErrorCodeEnum error)
        {
            var package = new WebPackage
            {
                Type = PackageTypeEnum.Act,
                ID = System.Threading.Interlocked.Increment(ref m_id),
                ActionId = action,
                Uid = uid,
                Room = room,
                Return = retData,
                ErrorCode = error,
            };
            return package;
        }
        public WebPackage CreatePackage(PackageTypeEnum ptype, int action, int uid, int romm, ErrorCodeEnum error)
        {
            var package = new WebPackage
            {
                Type = ptype,
                ID = System.Threading.Interlocked.Increment(ref m_id),
                ActionId = action,
                Uid = uid,
                ErrorCode = error,
            };
            return package;
        }
    }
}
