using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toys.NetWork;

namespace Toys.NetWork
{
    public class UserToken : NetWorkBase
    {
        /// <summary>
        /// 客户端IP
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                return Socket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// 发送心跳间隔时间
        /// </summary>
        public int TokenTime { get; set; }

        /// <summary>
        /// 是否已完成握手
        /// </summary>
        public bool WsShakeHand { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public NetType NetType { get; }

        /// <summary>
        /// 发送事件
        /// </summary>
        private SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();

        /// <summary>
        /// 接收事件
        /// </summary>
        private SocketAsyncEventArgs ReceiveArgs = new SocketAsyncEventArgs();

        /// <summary>
        /// 回调
        /// </summary>
        private Action<string, string> Disconnect { get; set; }

        /// <summary>
        /// 组包对象
        /// </summary>
        private IDataProcessor Processor { get; }

        /// <summary>
        /// 消息发送队列
        /// </summary>
        private Queue<byte[]> SendLine = new Queue<byte[]>();

        /// <summary>
        /// 防止内存溢出
        /// </summary>
        private int SendSyncCount { get; set; }

        /// <summary>
        /// 防止内存溢出
        /// </summary>
        private int ReciveSyncCount { get; set; }

        /// <summary>
        /// 可发送
        /// </summary>
        private bool SendAvailable { get; set; }

        /// <summary>
        /// 是否已经启动token线程
        /// </summary>
        private bool HasToken { get; set; }

        /// <summary>
        /// 初始化异步拆包及异步发送 接收由用户连接后ServerBiz启动
        /// </summary>
        public UserToken(Action<string, string> disconnect, IDataProcessor Processor, NetType netType)
        {
            NetType = netType;
            TokenTime = 10000;
            this.Disconnect = disconnect;
            this.Processor = Processor;
            SetAsynEventArgs();
        }

        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool Send(string data)
        {
            try
            {
                byte[] buffer = null;
                if (NetType == NetType.TcpType)
                {
                    buffer = ConvertMsgToByte(Encoding.UTF8.GetBytes(data));
                }
                else
                {
                    buffer = WebSocketConverter.PackData(data);
                }
                return Push(buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine("UserToken Send " + e.Message);
                Reset("UserToken Send " + e.Message);
                return false;
            }
        }


        /// <summary>
        /// 传入数组字节
        /// </summary>
        /// <param name="buffer"></param>
        public override bool Send(byte[] buffer)
        {
            try
            {
                if (NetType == NetType.TcpType)
                {
                    buffer = ConvertMsgToByte(buffer);
                }
                else
                {
                    //websocket模式下  如果还没有握手成功则直接发送握手信息 不再打包
                    if (WsShakeHand)
                    {
                        buffer = WebSocketConverter.PackBuffer(buffer);
                    }
                }
                return Push(buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine("UserToken Send buffer" + e.Message);
                Reset("UserToken Send buffer" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        /// <param name="buffer"></param>
        private void PushSystemData(byte[] buffer)
        {
            buffer = ConvertMsgToByte(buffer, true);
            Push(buffer);
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool Push(byte[] buffer)
        {
            try
            {
                bool flag = false;
                lock (SendLine)
                {
                    //IOCP为闲置可用状态
                    // 这样设计 为了节省一个线程 不用通过唤醒来 通知有消息需要发送 只要可以发送就直接发送，不能直接发送加入队列， 进入队列以后 等前面的消息发送完成 后面的 自然会被继续发送出去
                    //如果单独开一个线程负责所有的发送的话  可能会出现IO未结束 反复的轮训是否能发送 SendAvailable， 也是要用线程同步安全  一样避免不了lock
                    if (SendAvailable)
                    {
                        flag = true;
                        SendAvailable = false;
                    }
                    else
                    {
                        //忙碌状态 加入队列
                        SendLine.Enqueue(buffer);
                    }
                }
                if (flag)
                {
                    SendAsync(buffer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" Push(byte[] buffer) " + e.Message);
                Reset(" Push(byte[] buffer) " + e.Message);
            }
            return IsAvailable;
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="data"></param>
        protected override void SendAsync(byte[] data)
        {
            try
            {
                //从消息队列获取一个待发送数据
                LastSend = data;
                //设置发送内容
                SendArgs.SetBuffer(LastSend, 0, LastSend.Length);
                //异步发送  如果没有回调事件 直接处理
                if (!Socket.SendAsync(SendArgs))
                {
                    if (SendSyncCount > 5)
                    {
                        SendSyncCount = 0;
                        Task.Factory.StartNew(() => SendCompleted());
                    }
                    else
                    {
                        SendSyncCount++;
                        SendCompleted();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SendAsync(byte[] data) " + e.Message);
                Reset("SendAsync(byte[] data) " + e.Message);
            }

        }


        protected override void ReceiveAsync()
        {
            try
            {
                byte[] buffer = new byte[ReceiveSize];
                //设置缓冲区
                ReceiveArgs.SetBuffer(buffer, 0, buffer.Length);
                //异步接收
                if (!Socket.ReceiveAsync(ReceiveArgs))
                {
                    //没有发起事件则直接处理
                    if (ReciveSyncCount > 5)
                    {
                        ReciveSyncCount = 0;
                        Task.Factory.StartNew(() => ReceiveCompleted());
                    }
                    else
                    {
                        ReciveSyncCount++;
                        ReceiveCompleted();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ReceiveAsync() " + e.Message);
                Reset("ReceiveAsync() " + e.Message);
            }
        }

        /// <summary>
        /// 设置异步Socket事件
        /// </summary>
        public void SetAsynEventArgs()
        {
            //事件对象数据赋值
            SendArgs.UserToken = this;
            ReceiveArgs.UserToken = this;
            //事件
            SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            ReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
        }

        /// <summary>
        /// 用户连接后分配时调用
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public void Set(Socket socket)
        {
            //通信套接字赋值
            Socket = socket;
            IsAvailable = true;
            SendAvailable = true;
            //连接时间
            LastTokenDateTime = DateTime.Now;
            ReceiveAsync();

            if (Processor.IsClient)
            {
                ClientBiz();
            }
        }

        /// <summary>
        /// 发送数据回调
        /// </summary>
        /// <param name="asyncResult"></param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (e.LastOperation == SocketAsyncOperation.Send)
                    {
                        SendSyncCount = 0;
                        SendCompleted();
                    }
                    else if (e.LastOperation == SocketAsyncOperation.Receive)
                    {
                        ReciveSyncCount = 0;
                        ReceiveCompleted();
                    }
                }
                else
                {
                    Reset("  OnIOCompleted: " + e.SocketError.ToString());
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("OnIOCompleted(object sender, SocketAsyncEventArgs e) " + err.Message);
                Reset("OnIOCompleted(object sender, SocketAsyncEventArgs e) " + err.Message);
            }
        }

        /// <summary>
        /// 完成后
        /// </summary>
        private void SendCompleted()
        {
            try
            {
                //此处需要加这个判断否则 某些情况下有问题
                if (SendArgs.BytesTransferred != 0)
                {
                    byte[] buffer = null;
                    lock (SendLine)
                    {
                        //获取队列数据
                        if (SendLine.Count == 0)
                        {
                            //没有数据重置外部调用
                            SendAvailable = true;
                        }
                        else
                        {
                            buffer = SendLine.Dequeue();
                        }
                    }

                    //有效
                    if (IsAvailable)
                    {
                        if (buffer != null)
                        {
                            //继续发送
                            SendAsync(buffer);
                        }
                    }
                }
                else
                {
                    Reset("SendArgs套接字操作中传输的字节数为0");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SendCompleted() " + e.Message);
                Reset("SendCompleted() " + e.Message);
            }

        }

        /// <summary>
        /// 接收信息回调处理
        /// </summary>
        /// <param name="userToken"></param>
        private void ReceiveCompleted()
        {
            try
            {
                //此处需要加这个判断否则 某些情况下有问题
                if (ReceiveArgs.BytesTransferred != 0)
                {
                    //添加到拆包队列
                    Processor.ReceiveEnQuene((new ReciveData() { Buffer = ReceiveArgs.Buffer, Actual = ReceiveArgs.BytesTransferred, UserToken = this }));
                    //客户端模式
                    if (Processor.IsClient)
                    {
                        lock (Processor)
                        {
                            Monitor.PulseAll(Processor);
                        }
                    }
                    //连接正常 继续异步接收
                    if (IsAvailable)
                    {
                        ReceiveAsync();
                    }
                }
                else
                {
                    Reset("ReceiveArgs套接字操作中传输的字节数为0");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ReceiveCompleted() " + e.Message);
                Reset("ReceiveCompleted() " + e.Message);
            }
        }

        /// <summary>
        /// 客户端反馈给服务端
        /// </summary>
        private void ClientBiz()
        {
            var b = BitConverter.GetBytes(Command.Login.CInt());
            PushSystemData(b);
        }

        /// <summary>
        /// 服务端反馈给客户端
        /// </summary>
        public void LoginFinish()
        {
            var b = BitConverter.GetBytes(Command.Login.CInt());
            PushSystemData(b);
        }

        /// <summary>
        /// 客户端发送心跳
        /// </summary>
        /// <param name="data"></param>
        public void Token(ReciveData data)
        {
            if (Processor.IsClient && !HasToken)
            {
                if (data.Command == Command.Login)
                {
                    HasToken = true;
                    Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            Thread.Sleep(TokenTime);
                            var buffer = BitConverter.GetBytes(Command.Token.CInt());
                            PushSystemData(buffer);
                        }
                    }, TaskCreationOptions.LongRunning);
                }
            }
        }

        /// <summary>
        /// 连接断开失效后调用的 释放资源
        /// </summary>
        /// <param name="isReLogin">true就要待发送的全部发送完毕后才释放</param>
        public void Reset(string msg)
        {
            if (Monitor.TryEnter(this))
            {
                try
                {
                    if (IsAvailable)
                    {
                        IsAvailable = false;
                        SendLine.Clear();
                        Disconnect.Invoke(SessionId, msg);
                        Processor.Capacity--;
                        SendArgs.Dispose();
                        ReceiveArgs.Dispose();
                        Socket.Close();
                        Socket.Dispose();
                        Socket = null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 断开连接的用户UserID : " + SessionId + " 错误：" + e.Message);
                }
                finally
                {
                    try
                    {
                        Monitor.Exit(this);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("UserToken  Reset" + e.Message);
                    }
                }
            }
        }
    }
}
