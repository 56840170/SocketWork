using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    /// <summary>
    /// 连接池
    /// </summary>
    public class TokenPool
    {
        /// <summary>
        /// 连接池
        /// </summary>
        private ConcurrentBag<UserToken> Pool = new ConcurrentBag<UserToken>();

        /// <summary>
        /// 剩余数量
        /// </summary>
        public int Count
        {
            get
            {
                return Pool.Count;
            }
        }

        /// <summary>
        /// 工作线程
        /// </summary>
        private List<IDataProcessor> Workeres = new List<IDataProcessor>();

        /// <summary>
        /// 最大工作线程数
        /// </summary>
        public int WorkerCount { get; private set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public NetType NetType { get; }


        private AppServer AppServer { get; }

        /// <summary>
        /// 初始化连接池
        /// </summary>
        /// <param name="processMessage"></param>
        /// <param name="connectionFailed"></param>
        /// <param name="count"></param>
        /// <param name="mainClientManager"></param>
        public TokenPool(NetType netType, AppServer appServer)
        {
            AppServer = appServer;
            NetType = netType;
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            WorkerCount = Environment.ProcessorCount;
            if (NetType == NetType.TcpType)
            {
                for (int i = 0; i < WorkerCount; i++)
                {
                    Workeres.Add(new ReceiveProcessor(AppServer.OnMessage, false, AppServer.SessionManager));
                }
            }
            else
            {
                for (int i = 0; i < WorkerCount; i++)
                {
                    Workeres.Add(new WsProcessor(AppServer.OnMessage, AppServer.SessionManager));
                }
            }

            Add(AppServer.MaxCapacity);
            Task.Factory.StartNew(() => Timer(), TaskCreationOptions.LongRunning);
        }

        private void Timer()
        {
            while (true)
            {
                Thread.Sleep(3000);
                try
                {
                    Add(AppServer.MaxCapacity - Count);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Token Timer :" + e.Message + e.StackTrace);
                }
            }
        }

        /// <summary>
        /// 保存一个连接对象
        /// </summary>
        /// <param name="userToken"></param>
        private void Push(UserToken userToken)
        {
            Pool.Add(userToken);
        }

        /// <summary>
        /// 获取一个用户对象
        /// </summary>
        /// <returns></returns>
        public bool GetToken(out UserToken item)
        {
            return Pool.TryTake(out item);
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="processMessage"></param>
        /// <param name="connectionFailed"></param>
        /// <param name="count"></param>
        private void Add(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                IDataProcessor reciveProcessor = Workeres.Find(x => x.Capacity <= Workeres.Min(xm => xm.Capacity));
                reciveProcessor.Capacity++;
                UserToken userToken = new UserToken(AppServer.OnDisconnected, reciveProcessor, NetType);
                Push(userToken);
            }
        }
    }
}
