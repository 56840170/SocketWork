using System;
using System.Collections.Generic;
using System.Text;

namespace Toys.NetWork
{
    public interface ISessionAdd
    {
        /// <summary>
        /// 添加一个会话
        /// </summary>
        /// <param name="userToken"></param>
        void AddSession(UserToken userToken);


        /// <summary>
        /// 新连接
        /// </summary>
        /// <param name="token"></param>
        void OnConnected(UserToken token);
    }
}
