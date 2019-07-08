using System;
using System.Collections.Generic;
using System.Text;

namespace Toys.NetWork
{
    public interface ISessionControl
    {
        /// <summary>
        /// 获取连接
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        bool GetClient(string sessionId, out UserToken v);
    }
}
