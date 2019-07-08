using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public abstract class AppServer
    {

        private TokenPool TokenPool { get; }

        /// <summary>
        /// 服务器监听
        /// </summary>
        private Socket Listener { get; set; }


        /// <summary>
        /// 监听地址
        /// </summary>
        private IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// IOCP
        /// </summary>
        private SocketAsyncEventArgs SAEA { get; set; }


        /// <summary>
        /// 最大负载
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// 查询间隔
        /// </summary>
        public int TokenCheckTime { get; set; }

        /// <summary>
        /// 过期标准
        /// </summary>
        public int TokenPassTime { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public NetType NetType { get; set; }

        /// <summary>
        /// 会话管理
        /// </summary>
        public SessionManager SessionManager { get; }

        public AppServer(IPEndPoint iPEndPoint, NetType netType)
        {
            NetType = netType;
            TokenCheckTime = 13000;
            TokenPassTime = 30000;
            MaxCapacity = 10000;
            IPEndPoint = iPEndPoint;
            SessionManager = new SessionManager(netType, this);
            TokenPool = new TokenPool(netType, this);
            Timer();
        }


        public abstract void OnConnected(UserToken userToken);

        public abstract void OnMessage(ReciveData data);

        public abstract void OnDisconnected(string userId, string msg);

        /// <summary>
        /// 定时执行的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        InitListener();
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine("定时1分钟 重新侦听接端口:" + IPEndPoint.ToString() + " " + error.Message);
                    }
                    Thread.Sleep(10000 * 6);
                }
            }, TaskCreationOptions.LongRunning);

        }

        /// <summary>
        /// 监听初始化
        /// </summary>
        private void InitListener()
        {
            if (SAEA != null && Listener != null)
            {
                SAEA.Dispose();
                Listener.Dispose();
            }
            SAEA = new SocketAsyncEventArgs();
            SAEA.Completed += AccepetCallBack;
            Listener = CT.GetTCPSocketInstance();
            Listener.Bind(IPEndPoint);
            //监听数量
            Listener.Listen(TokenPool.Count);
            StartAccept(SAEA);
        }

        private int AccpetEventCount = 0;

        /// <summary>
        /// 接受客户端连接
        /// </summary>
        /// <param name="obj">Socket</param>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            if (!Listener.AcceptAsync(e))
            {
                //没有事件 直接结束 计数5次后线程 递归内存溢出
                if (AccpetEventCount > 5)
                {
                    AccpetEventCount = 0;
                    Task.Factory.StartNew(() => AcceptProcess(e));
                }
                else
                {
                    AccpetEventCount++;
                    AcceptProcess(e);
                }
            }
        }


        /// <summary>
        /// 侦听回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AccepetCallBack(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                AcceptProcess(e);
            }
            catch (Exception error)
            {
                Console.WriteLine("AccepetCallBack" + error.Message);
            }
        }

        /// <summary>
        /// 处理连接客户端
        /// </summary>
        /// <param name="e"></param>
        private void AcceptProcess(SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Accept && e.SocketError == SocketError.Success)
            {
                AccpetEventCount = 0;
                Socket socket = e.AcceptSocket;
                //没有满载继续接收
                if (TokenPool.Count > 0)
                {
                    CreateSession(socket);
                    StartAccept(e);
                }
            }
        }


        /// <summary>
        /// 获取tokend
        /// </summary>
        /// <param name="socket"></param>
        private async void CreateSession(Socket socket)
        {
            await Task.Run(() =>
            {
                if (TokenPool.GetToken(out UserToken userToken))
                {
                    userToken.Set(socket);
                }
            });
            Console.WriteLine("接收到了客户端：" + socket.RemoteEndPoint.ToString() + "的连接");
        }
    }
}
