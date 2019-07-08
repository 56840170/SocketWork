using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Toys.NetWork
{
    public abstract class NetWorkBase
    {
        /// <summary>
        /// TCP头部长度
        /// </summary>
        public const int TcpHeadLength = 40;

        /// <summary>
        /// 接收长度
        /// </summary>
        public const int ReceiveSize = 1024 * 512 /*65536*/;

        /// <summary>
        /// 通讯套接字
        /// </summary>
        protected Socket Socket { get; set; }

        /// <summary>
        /// 上次发送的
        /// </summary>
        protected byte[] LastSend { get; set; }

        /// <summary>
        /// 数据收集
        /// </summary>
        public List<byte> DataList { get; set; }

        /// <summary>
        /// 当前所需包长度
        /// </summary>
        public int CurrentPackageLength { get; set; }

        /// <summary>
        /// 上次的心跳包接收时间
        /// </summary>
        public DateTime LastTokenDateTime { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 用户唯一标识
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// 发送字节
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract bool Send(byte[] buffer);

        /// <summary>
        /// 发送字节
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract bool Send(string data);

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="data"></param>
        protected abstract void SendAsync(byte[] data);


        /// <summary>
        /// 异步接收
        /// </summary>
        protected abstract void ReceiveAsync();


        /// <summary>
        /// 将消息类转换成协议的字节数组
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dataType"></param>
        /// <returns>可直接发送的字节数组</returns>
        protected byte[] ConvertMsgToByte(byte[] buffer, bool IsSystem = false)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(SessionId ?? Guid.NewGuid().ToString("N")));
            list.AddRange(BitConverter.GetBytes(IsSystem ? 1 : 2));
            list.AddRange(BitConverter.GetBytes(buffer.Length));
            list.AddRange(buffer);
            return list.ToArray();
        }


        public NetWorkBase()
        {
            DataList = new List<byte>();
        }
    }
}
