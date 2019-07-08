using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public class UToken : UBasic
    {
        /// <summary>
        /// 连接对象
        /// </summary>
        protected Socket Socket { get; private set; }

        /// <summary>
        /// 接收事件
        /// </summary>
        private SocketAsyncEventArgs ReceiveArgs { get; set; }

        /// <summary>
        /// 发送事件
        /// </summary>
        private SocketAsyncEventArgs SendArgs { get; set; }

        /// <summary>
        /// 处理器
        /// </summary>
        private UdpProcessor UReciveProcessor { get; }

        /// <summary>
        /// 发送队列
        /// </summary>
        private Queue<byte[]> SendLine { get; set; }

        /// <summary>
        /// 发送是否空闲
        /// </summary>
        private bool SendAvailable { get; set; }

        /// <summary>
        /// 信息对象
        /// </summary>
        private IUdpHandler UdpHandler { get; }


        public UToken(IPEndPoint remote, UdpProcessor processor, IUdpHandler udpHandler) : base(remote)
        {
            UdpHandler = udpHandler;
            UReciveProcessor = processor;
            Nat();
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.LastOperation == SocketAsyncOperation.SendTo)
                {
                    SendCompleted();
                }

                else if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
                {
                    ReceiveCompleted();
                }
            }
        }

        /// <summary>
        /// 此处端口号需要进一步配置
        /// </summary>
        /// <param name="port"></param>
        private void Nat(int port = 0)
        {
            try
            {
                if (port > 65500 || port < 60020)
                {
                    port = 60020;
                }
                Socket = CT.GetUDPSocketInstance();

                Socket.Bind(new IPEndPoint(IPAddress.Any, port));
                NatAction();
            }
            catch
            {
                Socket.Close();
                Socket.Dispose();
                Nat(++port);
            }
        }

        private void Init()
        {
            SendLine = new Queue<byte[]>();
            SendAvailable = true;

            ReceiveArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = Remote
            };
            ReceiveArgs.Completed += IO_Completed;

            SendArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = Remote
            };

            SendArgs.Completed += IO_Completed;
            Receive();
            UdpHandler.OnConnectd(this);
        }


        private async void NatAction()
        {
            await Task.Factory.StartNew(() =>
             {
                 byte[] buffer = new byte[1024];
                 EndPoint refIP = new IPEndPoint(IPAddress.Any, 0);
                 Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref refIP, (ar) =>
                 {
                     int count = Socket.EndReceiveFrom(ar, ref refIP);
                     if (refIP.ToString() == UdpServer.NatIP.ToString())
                     {
                         //获取自己的外网IP
                         var result = Encoding.UTF8.GetString(buffer, 0, count);
                         var ipArr = result.Split(':');
                         var ip = new IPEndPoint(IPAddress.Parse(ipArr[0]), Convert.ToInt32(ipArr[1]));
                         //var dropMsg = Encoding.UTF8.GetBytes("打洞");
                         byte[] dropMsg = new byte[1024];
                         //给NAT留一条记录 以便客户端可以穿透Nat
                         Socket.SendTo(dropMsg, 0, dropMsg.Length, SocketFlags.None, Remote);
                         //如果NAT成功 就可以开始正式接受客户端的信息
                         byte[] flag = new byte[1024];
                         EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                         Socket.BeginReceiveFrom(flag, 0, flag.Length, SocketFlags.None, ref remote, (aresult) =>
                         {
                             try
                             {
                                 if (Socket.EndReceiveFrom(aresult, ref remote) == 112)
                                 {
                                     //Console.WriteLine(remote.ToString());
                                     Socket.SendTo(new byte[113], 0, 113, SocketFlags.None, Remote);
                                     IsAvailable = true;
                                     Init();
                                     Console.WriteLine("成功");
                                 }
                             }
                             catch (Exception e)
                             {
                                 Console.WriteLine("不允许打洞 :" + e.Message);
                                 Dispose("不允许打洞");
                             }

                         }, null);
                         //通过NAT连接返回自己的IP给客户端
                         UdpHandler.ReceptionSend(buffer, count, Remote);
                     }
                 }, null);
                 byte[] requestIp = new byte[110];
                 Socket.SendTo(requestIp, 0, requestIp.Length, SocketFlags.None, UdpServer.NatIP);
             });
        }

        /// <summary>
        /// 继续接收
        /// </summary>
        protected void ReceiveCompleted()
        {
            if (ReceiveArgs.BytesTransferred > 0)
            {
                UReciveProcessor.ReceiveEnQueue(new ReceiveByte() { Buffer = ReceiveArgs.Buffer, UToken = this, Count = ReceiveArgs.BytesTransferred });
                if (IsAvailable)
                {
                    Receive();
                }
            }
            else
            {
                Dispose("ReceiveArgs 接收字节数小于 0 ");
            }
        }

        protected void SendCompleted()
        {
            if (SendArgs.BytesTransferred > 0)
            {
                byte[] buffer = null;
                lock (SendLine)
                {
                    //获取队列数据
                    if (SendLine.Count <= 0)
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
                        Send(buffer);
                    }
                }
            }
            else
            {
                Dispose("SendArgs 小于0");
            }
        }

        public override void Dispose(string msg)
        {
            if (Monitor.TryEnter(this))
            {
                try
                {
                    if (Remote != null)
                    {
                        Remote = null;
                        IsAvailable = false;
                        ReceiveArgs.Dispose();
                        SendArgs.Dispose();
                        Socket.Close();
                        Socket.Dispose();
                        Console.WriteLine("Dispose " + msg);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Dispose " + e.Message);
                }
                finally
                {
                    try
                    {
                        Monitor.Exit(this);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("UToken Dispose" + e.Message);
                    }
                }
            }
        }

        protected override void Send(byte[] buffer)
        {
            try
            {
                //设置发送内容
                SendArgs.SetBuffer(buffer, 0, buffer.Length);
                //异步发送  如果没有回调事件 直接处理
                if (!Socket.SendToAsync(SendArgs))
                {
                    SendCompleted();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("UToken-send : " + e.Message);
            }
        }


        protected override void Receive()
        {
            try
            {
                byte[] b = new byte[65535];
                ReceiveArgs.SetBuffer(b, 0, b.Length);
                //直接完成
                if (!Socket.ReceiveFromAsync(ReceiveArgs))
                {
                    ReceiveCompleted();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Receive " + e.Message);
            }

        }

        public override bool PushDataIntoLine(byte[] buffer)
        {
            bool flag = false;
            lock (SendLine)
            {
                if (SendAvailable)
                {
                    SendAvailable = false;
                    flag = true;
                }
                else
                {
                    SendLine.Enqueue(buffer);
                }
            }

            if (flag)
            {
                Send(buffer);
            }
            return IsAvailable;
        }
    }
}
