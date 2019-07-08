using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Toys.NetWork
{
    public abstract class DataProcessor : IDataProcessor
    {

        /**
         * 
         * 如果使用多个线程互助的模式，  会出现为保证数据顺序  需要用到同步机制 影响性能 引起线程阻塞
         * 在大量数据并发时 影响峰值性能
         * 因此要在负载均衡方面入手
         *   
         * 
         * 
         * 
         * */

        /// <summary>
        /// 负载数量
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// 服务
        /// </summary>
        protected Action<ReciveData> Process { get; set; }

        /// <summary>
        /// 是否是客户端
        /// </summary>
        public bool IsClient { get; protected set; }

        /// <summary>
        /// 添加会话
        /// </summary>
        public ISessionAdd ISessionAdd { get; }

        /// <summary>
        /// 任务队列
        /// </summary>
        protected readonly ConcurrentQueue<ReciveData> MQ = new ConcurrentQueue<ReciveData>();

        /// <summary>
        /// 可读消息队列
        /// </summary>
        protected readonly ConcurrentQueue<ReciveData> MCQ = new ConcurrentQueue<ReciveData>();


        public DataProcessor(ISessionAdd sessionAdd)
        {
            ISessionAdd = sessionAdd;
        }


        public void ReceiveEnQuene(ReciveData reciveData)
        {
            MQ.Enqueue(reciveData);
        }

    }
}
