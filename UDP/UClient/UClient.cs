using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Toys.NetWork
{
    public class UClient : UBasic
    {
        /// <summary>
        /// UDP连接
        /// </summary>
        private UdpClient UdpClient { get; set; }

        /// <summary>
        /// 发送队列
        /// </summary>
        private Queue<byte[]> SendLine { get; set; }

        /// <summary>
        /// 处理队列
        /// </summary>
        private Queue<byte[]> MessageLine { get; set; }

        /// <summary>
        /// 发布消息
        /// </summary>
        public event Action<byte[]> InvokeMessage;

        /// <summary>
        /// 准备
        /// </summary>
        private bool SendFlag, ReceiveFlag, ProcessFlag;

        public UClient(IPEndPoint remote) : base(remote)
        {
            SendLine = new Queue<byte[]>();

            MessageLine = new Queue<byte[]>();
            //绑定端口
            Bind(30303);
            //接收返回的地址
            Nat();
        }

        /// <summary>
        /// 工作线程
        /// </summary>
        public void InitTask()
        {
            Task.Factory.StartNew(() => Send(null));
            Task.Factory.StartNew(() => Receive());
            Task.Factory.StartNew(() => ProcessData(), TaskCreationOptions.LongRunning);
            while (!SendFlag || !ReceiveFlag || !ProcessFlag) { Thread.Sleep(50); }
        }

        /// <summary>
        /// 绑定端口
        /// </summary>
        /// <param name="port"></param>
        private void Bind(int port)
        {
            try
            {
                UdpClient = new UdpClient();
                UdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch
            {
                UdpClient.Client.Dispose();
                UdpClient.Dispose();
                Bind(++port);
            }
        }

        /// <summary>
        /// 循环发送
        /// </summary>
        protected override void Send(byte[] data)
        {
            try
            {
                lock (SendLine)
                {
                    if (SendLine.Count > 0)
                    {
                        data = SendLine.Dequeue();
                    }
                    else
                    {
                        SendFlag = true;
                        Monitor.Wait(SendLine);
                    }
                }

                if (data != null)
                {
                    UdpClient.BeginSend(data, data.Length, Remote, (ar) =>
                    {
                        if (UdpClient.EndSend(ar) == data.Length)
                        {
                            ar.AsyncWaitHandle.Dispose();
                            Send(null);
                        }
                    }, null);
                }
                else
                {
                    Send(null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Dispose("Send " + e.Message);
            }
        }

        /// <summary>
        /// 循环接收
        /// </summary>
        protected override void Receive()
        {
            try
            {
                UdpClient.BeginReceive((ar) =>
                {
                    IPEndPoint iPEnd = null;
                    byte[] data = UdpClient.EndReceive(ar, ref iPEnd);
                    ar.AsyncWaitHandle.Dispose();
                    if (data.Length > 0)
                    {
                        lock (MessageLine)
                        {
                            MessageLine.Enqueue(data);
                            Monitor.PulseAll(MessageLine);
                        }
                        Receive();
                    }
                }, null);
                ReceiveFlag = true;
            }
            catch (Exception e)
            {
                Dispose("Receive " + e.Message);
            }
        }

        /// <summary>
        /// 消息数据处理
        /// </summary>
        private void ProcessData()
        {
            while (IsAvailable)
            {
                try
                {
                    byte[] data = null;
                    lock (MessageLine)
                    {
                        if (MessageLine.Count > 0)
                        {
                            data = MessageLine.Dequeue();
                        }
                        else
                        {
                            ProcessFlag = true;
                            Monitor.Wait(MessageLine);
                        }
                    }

                    if (data != null)
                    {
                        InvokeMessage?.Invoke(data);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Dispose("ProcessData " + e.Message);
                }
            }
        }


        private void Hole(IPEndPoint iPEndPoint, byte[] buffer)
        {
            if (iPEndPoint.CString() == Remote.CString())
            {
                //返回的IP
                var ip = System.Text.Encoding.UTF8.GetString(buffer);
                Console.WriteLine(iPEndPoint + " " + ip);
                var arr = ip.Split(':');
                IPEndPoint remote = new IPEndPoint(IPAddress.Parse(arr[0]), Convert.ToInt32(arr[1]));
                //先接收
                UdpClient.BeginReceive((aarr) =>
                {
                    IPEndPoint temp = null;
                    var fialResult = UdpClient.EndReceive(aarr, ref temp);
                    if (fialResult.Length == 113)
                    {
                        IsAvailable = true;
                        Console.WriteLine("打通");
                        Remote = remote;
                        InitTask();
                    }
                }, null);
                var start = new byte[112];
                //顺着通道建立连接
                UdpClient.Send(start, start.Length, remote);
            }
            else
            {
                UdpClient.BeginReceive(WaitForEnd, null);
            }
        }


        private void WaitForEnd(IAsyncResult ar)
        {
            IPEndPoint ipend = null;
            byte[] buffer = UdpClient.EndReceive(ar, ref ipend);
            Hole(ipend, buffer);
        }

        /// <summary>
        /// MAT穿透
        /// </summary>
        private void Nat()
        {
            UdpClient.BeginReceive(WaitForEnd, null);
            var nat = new byte[111];
            int count = UdpClient.Send(nat, nat.Length, Remote);
        }

        public override void Dispose(string msg)
        {
            Console.WriteLine(msg);
            try
            {
                if (Remote != null)
                {
                    lock (this)
                    {
                        if (Remote != null)
                        {
                            Remote = null;
                            IsAvailable = false;
                            UdpClient.Close();
                            UdpClient.Dispose();
                            SendLine.Clear();
                            MessageLine.Clear();
                        }
                    }
                }


            }
            catch (Exception e)
            {
                Console.WriteLine("Dispose " + e.Message);
            }

        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public override bool PushDataIntoLine(byte[] buffer)
        {
            if (IsAvailable)
            {
                lock (SendLine)
                {
                    SendLine.Enqueue(buffer);
                    Monitor.PulseAll(SendLine);
                }
                return true;
            }
            return false;
        }
    }
}
