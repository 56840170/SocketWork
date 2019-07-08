using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toys.NetWork
{
    public class AppSession
    {

        public IPEndPoint IPEndPoint { get; }


        public UserToken UserToken { get; }


        private ReceiveProcessor ReceiveProcessor { get; }


        public event Action<ReciveData> OnMessage;


        public event Action<ReciveData> OnConnected;


        public event Action<string> OnDisconnect;



        public AppSession(IPEndPoint iPEndPoint)
        {
            IPEndPoint = iPEndPoint;

            ReceiveProcessor = new ReceiveProcessor(OnMessages, true, null);

            UserToken = new UserToken(Disconnect, ReceiveProcessor, NetType.TcpType);

            var socket = CT.GetTCPSocketInstance();

            socket.Connect(iPEndPoint);

            UserToken.Set(socket);
        }


        private void OnMessages(ReciveData data)
        {
            if (data.Command == Command.Login)
            {

                OnConnected?.Invoke(data);

            }
            else
            {
                OnMessage?.Invoke(data);
            }
        }

        private void Disconnect(string id, string reason)
        {
            OnDisconnect?.Invoke(reason);
        }

    }
}
