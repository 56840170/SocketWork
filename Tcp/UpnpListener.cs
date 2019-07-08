//using NATUPNPLib;
//using NetWork;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Toys.NetWork
//{
//    public static class UpnpListener
//    {
//        /// <summary>
//        /// UPNP映射端口 内外共用
//        /// </summary>
//        private static int Port = 43999;

//        /// <summary>
//        /// 主机名
//        /// </summary>
//        private static readonly string HostName = Dns.GetHostName();

//        /// <summary>
//        /// 本机IP 断网后再分配DHCP有问题
//        /// </summary>
//        private static readonly IPAddress LocalIP = Dns.GetHostEntry(HostName).AddressList.Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

//        /// <summary>
//        /// UPnP协议接口
//        /// </summary>
//        private static readonly UPnPNAT UPnPNAT = new UPnPNAT();

//        /// <summary>
//        /// 映射地址
//        /// </summary>
//        public static IPEndPoint IPEndPoint { get; set; }

//        /// <summary>
//        /// 监听Socket
//        /// </summary>
//        private static Socket P2PServerWatch = CT.GetTCPSocketInstance();

//        /// <summary>
//        /// 初始化
//        /// </summary>
//        public static bool Init()
//        {
//            try
//            {
//                if (UPnPNAT.StaticPortMappingCollection == null)
//                {
//                    Console.WriteLine("没有检测到路由器，或者路由器不支持UPnP功能");
//                    return false;
//                }
//                UPnPNAT.StaticPortMappingCollection.Add(Port, "TCP", Port, LocalIP.ToString(), true, "老刘P2P传输");
//                string WanIP = CT.GetWanIP();
//                string[] ipPort = WanIP.Split(':');
//                IPEndPoint = new IPEndPoint(IPAddress.Parse(ipPort[0]), Port);
//                P2PServerWatch.Bind(new IPEndPoint(IPAddress.Parse(LocalIP.ToString()), Port));
//                P2PServerWatch.Listen(1000);
//                Console.WriteLine("外网IP：" + WanIP + ":" + Port);
//                return true;
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine("UPnPTCPListen:" + e.Message);
//                Port++;
//                return Init();
//            }
//        }
//    }
//}


