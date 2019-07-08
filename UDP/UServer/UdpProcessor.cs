using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public class UdpProcessor
    {

        /// <summary>
        /// 任务队列
        /// </summary>
        private readonly ConcurrentQueue<ReceiveByte> MQ = new ConcurrentQueue<ReceiveByte>();

        /// <summary>
        /// 可读消息队列
        /// </summary>
        private readonly ConcurrentQueue<ReceiveByte> MCQ = new ConcurrentQueue<ReceiveByte>();

        /// <summary>
        /// udp消息
        /// </summary>
        private IUdpHandler IUdpHandler { get; }


        public UdpProcessor(IUdpHandler udpHandler)
        {
            IUdpHandler = udpHandler;
            Task.Factory.StartNew(() => ProcessByte(), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 数据执行
        /// </summary>
        private void ProcessByte()
        {
            while (true)
            {
                try
                {
                    if (MQ.TryDequeue(out ReceiveByte receive))
                    {
                        IUdpHandler.OnMessage(receive);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ProcessByte " + e.Message);
                }
            }
        }

        /// <summary>
        /// 待处理入队
        /// </summary>
        /// <param name="receive"></param>
        public void ReceiveEnQueue(ReceiveByte receive)
        {
            MQ.Enqueue(receive);
        }
    }
}
