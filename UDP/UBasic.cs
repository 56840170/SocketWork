using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public abstract class UBasic
    {

        /// <summary>
        /// 对方地址
        /// </summary>
        public IPEndPoint Remote { get; protected set; }

        /// <summary>
        /// 连接是否可用
        /// </summary>
        public bool IsAvailable { get; protected set; }




        public UBasic(IPEndPoint remote)
        {
            Remote = remote;
        }

        protected abstract void Send(byte[] buffer);
        protected abstract void Receive();
        public abstract bool PushDataIntoLine(byte[] buffer);
        public abstract void Dispose(string msg);
    }
}
