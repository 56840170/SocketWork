using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public abstract class UdpServer : IUdpHandler
    {
        /// <summary>
        /// 接待请求的
        /// </summary>
        private UdpClient ReceptionUdp { get; set; }

        /// <summary>
        /// 接待请求的地址
        /// </summary>
        public IPEndPoint ReceptionIP { get; }

        /// <summary>
        /// 地址转换的
        /// </summary>
        private UdpClient NatUdp { get; }

        /// <summary>
        /// 地址转换的连接地址
        /// </summary>
        public static IPEndPoint NatIP { get; private set; }

        /// <summary>
        /// 处理器
        /// </summary>
        private UdpProcessor[] Processors { get; }


        private int Index { get; set; }


        public Action<ReceiveByte> ReceiveEnQueue { get; set; }


        public UdpServer(int receptionPort, int natPort, string host)
        {
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            Processors = new UdpProcessor[Environment.ProcessorCount];
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                Processors[i] = new UdpProcessor(this);
            }

            ReceptionUdp = new UdpClient();
            ReceptionUdp.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            ReceptionUdp.Client.Bind(new IPEndPoint(IPAddress.Any, receptionPort));
            ReceptionIP = CT.TCPServerEndPoint(host, receptionPort);
            NatUdp = new UdpClient();
            NatUdp.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            NatUdp.Client.Bind(new IPEndPoint(IPAddress.Any, natPort));
            NatIP = CT.TCPServerEndPoint(host, natPort);
            NatRecevie();
            Receive();
        }

        /// <summary>
        /// 接待新连接
        /// </summary>
        public void Receive()
        {
            ReceptionUdp.BeginReceive((ar) =>
            {
                try
                {
                    IPEndPoint ip = null;
                    byte[] result = ReceptionUdp.EndReceive(ar, ref ip);
                    if (result.Length == 111)
                    {
                        Console.WriteLine(ip.ToString());
                        if (Index == Processors.Length)
                        {
                            Index = 0;
                        }
                        var token = new UToken(ip, Processors[Index], this);
                        Index++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Receive " + e.Message);
                }
                Receive();
            }, null);
            Console.WriteLine("接待中。。。");
        }

        /// <summary>
        /// 返回一个NAT后的地址
        /// </summary>
        public void NatRecevie()
        {
            NatUdp.BeginReceive((ar) =>
            {
                try
                {
                    IPEndPoint ip = null;
                    byte[] result = NatUdp.EndReceive(ar, ref ip);
                    if (result.Length == 110)
                    {
                        var buffer = Encoding.UTF8.GetBytes(ip.ToString());
                        var i = NatUdp.Send(buffer, buffer.Length, ip);
                        if (buffer.Length != i)
                        {
                            Console.WriteLine("NatRecevie 发送错误");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("NatRecevie " + e.Message);
                }
                NatRecevie();
            }, null);
        }


        public void ReceptionSend(byte[] buffer, int count, IPEndPoint Remote)
        {
            lock (ReceptionUdp)
            {
                ReceptionUdp.Send(buffer, count, Remote);
            }
        }

        public abstract void OnConnectd(UToken uToken);

        public abstract void OnMessage(ReceiveByte receiveByte);
    }
}
