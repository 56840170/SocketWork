using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public class ReceiveByte
    {
        /// <summary>
        /// token
        /// </summary>
        public UToken UToken { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public int Count { get; set; }
    }
}
