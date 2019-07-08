using System;
using System.Collections.Generic;
using System.Text;

namespace Toys.NetWork
{
    public interface IDataProcessor
    {
        /// <summary>
        /// 是否客户端逻辑
        /// </summary>
        bool IsClient { get; }

        /// <summary>
        /// 负载数
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// 添加会话
        /// </summary>
        ISessionAdd ISessionAdd { get; }


        /// <summary>
        /// 接收到的数据加入到处理队列
        /// </summary>
        /// <param name="reciveData"></param>
        void ReceiveEnQuene(ReciveData reciveData);
    }
}
