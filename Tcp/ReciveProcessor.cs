using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public class ReceiveProcessor : DataProcessor
    {

        /// <summary>
        /// 线程
        /// </summary>
        /// <param name="server"></param>
        public ReceiveProcessor(Action<ReciveData> server, bool isClient, ISessionAdd sessionAdd) : base(sessionAdd)
        {
            IsClient = isClient;
            Process = server;
            Task.Factory.StartNew(() => RebuildData(), TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() => InvokeMessage(), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 重组数据
        /// </summary>
        public void RebuildData()
        {
            while (true)
            {
                ReciveData data = null;
                try
                {
                    if (MQ.TryDequeue(out data))
                    {
                        if (!ProcessByte(data))
                        {
                            data.UserToken.Reset("CollectReceiveData 复原数据出错");
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                        if (IsClient)
                        {
                            lock (this)
                            {
                                Monitor.Wait(this);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (data != null)
                    {
                        data.UserToken.Reset("RebuildData:" + e.Message + e.StackTrace);
                    }
                }
            }

        }

        /// <summary>
        /// 信息处理
        /// </summary>
        /// <param name="server"></param>
        public void InvokeMessage()
        {
            while (true)
            {
                ReciveData data = null;
                try
                {
                    if (MCQ.TryDequeue(out data))
                    {
                        //登陆保存会话 此处已经分配了SESSIONID
                        if (data.Command == Command.Login && ISessionAdd != null)
                        {
                            ISessionAdd.AddSession(data.UserToken);
                            data.UserToken.LoginFinish();
                            ISessionAdd.OnConnected(data.UserToken);
                        }
                        else if (data.Command == Command.Token)
                        {
                            data.UserToken.LastTokenDateTime = DateTime.Now;
                        }
                        else
                        {
                            //客户端定时发送心跳
                            data.UserToken.Token(data);
                            Process.Invoke(data);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (Exception e)
                {
                    if (data != null)
                    {
                        data.UserToken.Reset("InvokeMessage:" + e.Message + e.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns>是否成功</returns>
        private bool ProcessByte(ReciveData data)
        {
            try
            {
                //将收到的数据存在缓冲区
                byte[] temp = new byte[data.Actual];
                Array.Copy(data.Buffer, 0, temp, 0, temp.Length);
                data.UserToken.DataList.AddRange(temp);

                //数据超过Tcp自定义头部长度
                if (data.UserToken.DataList.Count > NetWorkBase.TcpHeadLength && data.UserToken.DataList.Count >= data.UserToken.CurrentPackageLength)
                {
                    //创建信息容器
                    List<ReciveData> list = new List<ReciveData>();
                    //处理完成后 不管连接是否失效 那信息都是有效的 
                    DecodeData(list, data.UserToken);
                    if (list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            //加入队列
                            MCQ.Enqueue(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                data.UserToken.Reset("处理接收到的数据:" + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 解包动作
        /// </summary>
        /// <param name="list"></param>
        /// <param name="userToken"></param>
        private void DecodeData(List<ReciveData> list, UserToken userToken)
        {
            try
            {
                //所有的字节转成数组用于操作拆包
                byte[] buffer = userToken.DataList.ToArray();
                if (string.IsNullOrEmpty(userToken.SessionId))
                {
                    if (!IsClient)
                    {
                        userToken.SessionId = Guid.NewGuid().ToString("N");
                    }
                    else
                    {
                        //收到用户唯一标识ID  用户每次发送都会拼在头部
                        userToken.SessionId = Encoding.UTF8.GetString(buffer, 0, 32);
                    }
                }
                //字节转有效数据
                int dataType = BitConverter.ToInt32(buffer, 32);
                //本次数据有效长度
                int dataLength = BitConverter.ToInt32(buffer, 36);
                //总包体长度
                int totalpackage = dataLength + NetWorkBase.TcpHeadLength;
                //如果数据头部加数据体的长度小于或者等于缓冲区buffer则解包
                userToken.CurrentPackageLength = totalpackage;
                //数据有达到总长度 则开始拆包
                if (totalpackage <= buffer.Length)
                {
                    //可拆包 则赋值为0 等下一个数据包
                    userToken.CurrentPackageLength = 0;
                    //拆包+清理缓存
                    CleanWare(buffer, totalpackage, dataLength, list, userToken, dataType);
                }
            }
            catch (Exception e)
            {
                userToken.Reset("解包动作:" + e.Message);
            }
        }

        /// <summary>
        /// 整理缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="totalpackage"></param>
        /// <param name="dataLength"></param>
        /// <param name="list"></param>
        /// <param name="userToken"></param>
        private void CleanWare(byte[] buffer, int totalpackage, int dataLength, List<ReciveData> list, UserToken userToken, int dataType)
        {
            try
            {
                //解包后剩下未处理的数据存放
                byte[] remain = new byte[buffer.Length - totalpackage];
                //数据实体
                byte[] data = new byte[dataLength];
                Array.Copy(buffer, NetWorkBase.TcpHeadLength, data, 0, data.Length);
                Array.Copy(buffer, totalpackage, remain, 0, remain.Length);
                //将剩下的字节放回接收缓存
                userToken.DataList = remain.ToList<byte>();
                ReciveData reciveData = new ReciveData()
                {
                    UserToken = userToken,
                    Command = dataType == 1 ? (Command)BitConverter.ToInt32(data) : Command.Nothing,
                    Actual = data.Length,
                    Buffer = data,
                };
                //添加到数据包list
                list.Add(reciveData);
                //剩余数据继续递归调用
                if (userToken.DataList.Count > NetWorkBase.TcpHeadLength)
                {
                    DecodeData(list, userToken);
                }
            }
            catch (Exception e)
            {
                userToken.Reset("CleanWare " + e.Message);
            }
        }
    }
}
