using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public class WsProcessor : DataProcessor
    {

        public WsProcessor(Action<ReciveData> server, ISessionAdd sessionAdd) : base(sessionAdd)
        {
            Process = server;
            Task.Factory.StartNew(() => InvokeMessage(), TaskCreationOptions.LongRunning);
        }

        public void InvokeMessage()
        {
            while (true)
            {
                Thread.Sleep(1);
                ReciveData data = null;
                try
                {
                    if (MQ.TryDequeue(out data))
                    {
                        if (!data.UserToken.WsShakeHand)
                        {
                            var shakingKey = WebSocketConverter.GetSecKeyAccetp(data.Buffer, data.Actual);
                            if (!string.IsNullOrEmpty(shakingKey))
                            {
                                var buffer = WebSocketConverter.PackHandShakeData(shakingKey);
                                if (data.UserToken.Send(buffer))
                                {
                                    data.UserToken.WsShakeHand = true;
                                    Console.WriteLine("已经发送握手协议了....");
                                }
                            }
                            else
                            {
                                data.UserToken.Reset("无效的协议");
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(data.UserToken.SessionId))
                            {
                                data.UserToken.SessionId = Guid.NewGuid().ToString("N");
                                ISessionAdd.AddSession(data.UserToken);
                                ISessionAdd.OnConnected(data.UserToken);
                            }
                            var (message, buffer) = WebSocketConverter.AnalyticData(data.Buffer, data.Actual);
                            data.Message = message;
                            data.Buffer = buffer;
                            data.Actual = buffer.Length;
                            Process.Invoke(data);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (data != null)
                    {
                        data.UserToken.Reset("WsProcessor " + e.Message);
                    }
                }
            }
        }
    }
}
