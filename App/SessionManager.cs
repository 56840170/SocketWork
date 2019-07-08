using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    /// <summary>
    /// 用户连接管理类
    /// </summary>
    public class SessionManager : ISessionControl, ISessionAdd
    {
        /// <summary>
        /// 此服务点的所有用户 
        /// </summary>
        private ConcurrentDictionary<string, UserToken> Sessiones = new ConcurrentDictionary<string, UserToken>();

        /// <summary>
        /// 服务器加载时间
        /// </summary>
        public DateTime OpenTime { get; }

        /// <summary>
        /// 当前连接数量
        /// </summary>
        public int Count
        {
            get
            {
                return Sessiones.Count;
            }
        }

        /// <summary>
        /// 服务器对象
        /// </summary>
        private AppServer AppServer { get; }

        /// <summary>
        /// 网络连接类型，以及服务器对象
        /// </summary>
        /// <param name="netType"></param>
        /// <param name="appServer"></param>
        public SessionManager(NetType netType, AppServer appServer)
        {
            OpenTime = DateTime.Now;
            AppServer = appServer;
            if (netType == NetType.TcpType)
            {
                Task.Factory.StartNew(() => TimerToRemove(), TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// 定时清除掉线用户
        /// </summary>
        private void TimerToRemove()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(AppServer.TokenCheckTime);
                    DateTime checkTime = DateTime.Now;
                    List<KeyValuePair<string, UserToken>> checkList = Sessiones.ToList();
                    foreach (var currentToken in checkList)
                    {
                        //超时没有更新心跳包信息 判定为掉线
                        if ((checkTime - currentToken.Value.LastTokenDateTime).TotalMilliseconds >= AppServer.TokenPassTime)
                        {
                            //用户下线
                            DisposeClient(currentToken.Key);
                            //LogHelper.LogInfo().Info("清理key:" + currentToken.Key + " 清理离线用户ID： " + currentToken.Value.UserID + " 当前连接人数：" + Count);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("TimerToRemove:" + e.Message + e.StackTrace);
                }
            }
        }

        /// <summary>
        /// 获取客户端连接对象
        /// </summary>
        /// <param name="sessionId">用户唯一标志UserID</param>
        /// <returns></returns>
        public bool GetClient(string sessionId, out UserToken v)
        {
            return Sessiones.TryGetValue(sessionId, out v);
        }

        /// <summary>
        /// 销毁对话
        /// </summary>
        /// <param name="sessionId"></param>
        private void DisposeClient(string sessionId)
        {
            //用户下线
            if (Sessiones.TryRemove(sessionId, out UserToken value))
            {
                value.Reset("用户下线");
            }
        }

        /// <summary>
        /// 线程安全的字典集合添加处理 clean为true 已存在的值做销毁处理
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cd"></param>
        /// <param name="value"></param>
        /// <param name="clean"></param>
        /// <returns></returns>
        private void AddOrUpdateClient(string key, UserToken value)
        {
            Sessiones.AddOrUpdate(key, value, (k, v) =>
            {
                v.Reset("不可能事情发生了，GUID居然重复了");
                return value;
            });
        }


        public void AddSession(UserToken userToken)
        {
            AddOrUpdateClient(userToken.SessionId, userToken);
        }

        public void OnConnected(UserToken token)
        {
            AppServer.OnConnected(token);
        }
    }
}
