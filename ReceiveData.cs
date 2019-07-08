using System;
using System.Collections.Generic;
using System.Text;

namespace Toys.NetWork
{
    public class ReciveData
    {
        /// <summary>
        /// token
        /// </summary>
        public UserToken UserToken { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// 实际接收字节数
        /// </summary>
        public int Actual { get; set; }

        /// <summary>
        /// 内部命令
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// websocket连接专用 有值
        /// </summary>
        public string Message { get; set; }

    }
}
