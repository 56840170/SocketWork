using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Toys.NetWork
{
    public interface IUdpHandler
    {
        /// <summary>
        /// 新连接
        /// </summary>
        /// <param name="uToken"></param>
        void OnConnectd(UToken uToken);

        /// <summary>
        /// 信息中转
        /// </summary>
        /// <param name="receiveByte"></param>
        void OnMessage(ReceiveByte receiveByte);

        /// <summary>
        /// 接待udpserver发送信息
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        /// <param name="Remote"></param>
        void ReceptionSend(byte[] buffer, int count, IPEndPoint Remote);
    }
}
