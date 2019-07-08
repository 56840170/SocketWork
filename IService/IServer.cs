using System;
using System.Collections.Generic;
using System.Text;

namespace Toys.NetWork
{
    public interface IServer
    {
        void OnDisconnected(string reson);
    }
}
