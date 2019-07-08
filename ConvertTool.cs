using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Toys.NetWork
{
    public static class CT
    {
        /// <summary>
        ///  DES加密
        /// </summary>
        public static Byte[] key = { 12, 23, 34, 45, 56, 67, 78, 255 };

        /// <summary>
        ///  DES加密
        /// </summary>
        public static Byte[] iv = { 120, 230, 10, 1, 10, 20, 30, 40 };


        /// <summary>
        /// 异或加密
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] EncryptDecrypt(byte[] buffer, byte key)
        {
            for (int i = 0; i < buffer.Length; i += 4)
            {

                buffer[i] = (byte)(buffer[i] ^ key);

            }
            return buffer;
        }

        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] MyDESCrypto(this byte[] data)
        {
            DESCryptoServiceProvider desc = new DESCryptoServiceProvider();
            MemoryStream mStream = new MemoryStream();
            ICryptoTransform transform = desc.CreateEncryptor(key, iv);//加密对象
            CryptoStream cStream = new CryptoStream(mStream, transform, CryptoStreamMode.Write);
            cStream.Write(data, 0, data.Length);
            cStream.FlushFinalBlock();
            //return Convert.ToBase64String(mStream.ToArray());
            return mStream.ToArray();
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] MyDESCryptoDe(this byte[] byts)
        {
            //解密
            //byte[] strs = Convert.FromBase64String(str);
            DESCryptoServiceProvider desc = new DESCryptoServiceProvider();
            MemoryStream mStream = new MemoryStream();
            ICryptoTransform transform = desc.CreateDecryptor(key, iv);//解密对象
            CryptoStream cStream = new CryptoStream(mStream, transform, CryptoStreamMode.Write);
            cStream.Write(byts, 0, byts.Length);
            cStream.FlushFinalBlock();
            return mStream.ToArray();
        }

        /// <summary>
        /// 对象转BYTE[]
        /// </summary>
        /// <param name="obj">转换的对象</param>
        /// <returns>字节数组</returns>
        public static byte[] ObjectToBytes(this object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, obj);
            byte[] data = stream.ToArray();
            stream.Close();
            return data;
        }

        /// <summary>
        /// 字节转对象
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T BytesToObject<T>(this byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                IFormatter formatter = new BinaryFormatter();
                object obj = formatter.Deserialize(stream);
                stream.Close();
                return (T)obj;
            }
        }

        /// <summary>
        /// md5加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string MD5(this string str)
        {
            try
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString().ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
            }
        }

        /// <summary>
        /// 文件获取MD5
        /// </summary>
        /// <param name="bytedata">字节数组</param>
        /// <returns></returns>
        public static string GetMD5Hash(this byte[] bytedata)
        {
            try
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(bytedata);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString().ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
            }
        }

        /// <summary>
        /// 获取TCPSocket对象
        /// </summary>
        /// <returns></returns>
        public static Socket GetTCPSocketInstance()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            return socket;
        }

        /// <summary>
        /// 获取TCPSocket对象
        /// </summary>
        /// <returns></returns>
        public static Socket GetUDPSocketInstance()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            return socket;
        }

        /// <summary>
        /// 获取服务器连接地址
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static IPEndPoint TCPServerEndPoint(string host, int port)
        {
            IPAddress[] IPs = Dns.GetHostAddresses(host);
            return new IPEndPoint(IPs[0], port);
        }

        /// <summary>
        /// 获取本地外网IP
        /// </summary>
        /// <returns></returns>
        public static string GetWanIP()
        {
            try
            {
                string tempip = "";
                WebRequest request = WebRequest.Create("http://ip.qq.com/");
                request.Timeout = 10000;
                WebResponse response = request.GetResponse();
                Stream resStream = response.GetResponseStream();
                StreamReader sr = new StreamReader(resStream, System.Text.Encoding.Default);
                string htmlinfo = sr.ReadToEnd();
                //匹配IP的正则表达式
                Regex r = new Regex("((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]\\d|\\d)\\.){3}(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]\\d|[1-9])", RegexOptions.None);
                Match mc = r.Match(htmlinfo);
                //获取匹配到的IP
                tempip = mc.Groups[0].Value;
                resStream.Close();
                sr.Close();
                return tempip;
            }
            catch
            {
                return GetWanIP();
            }
        }

        /// <summary>
        /// int转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int CInt(this object obj)
        {
            try
            {
                return Convert.ToInt32(obj);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// long转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static long CLong(this object obj)
        {
            try
            {
                return Convert.ToInt64(obj);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 字符串转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string CString(this object obj)
        {
            try
            {
                return Convert.ToString(obj);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>  
        /// 获取网络日期时间  
        /// </summary>  
        /// <returns></returns>  
        public static DateTime GetNetDateTime()
        {
            WebRequest request = null;
            WebResponse response = null;
            WebHeaderCollection headerCollection = null;
            string datetime = string.Empty;
            try
            {
                request = WebRequest.Create("https://www.baidu.com");
                request.Timeout = 3000;
                request.Credentials = CredentialCache.DefaultCredentials;
                response = (WebResponse)request.GetResponse();
                headerCollection = response.Headers;
                foreach (var h in headerCollection.AllKeys)
                { if (h == "Date") { datetime = headerCollection[h]; } }
                return Convert.ToDateTime(datetime);
            }
            catch (Exception)
            {
                return GetNetDateTime();
            }
            finally
            {
                if (request != null)
                { request.Abort(); }
                if (response != null)
                { response.Close(); }
                if (headerCollection != null)
                { headerCollection.Clear(); }
            }
        }

        /// <summary>
        /// 获取10位时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTime()
        {
            long time = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            return time;
        }

        /// <summary>
        /// 检查IP地址格式
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsIP(string ip)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }
    }
}
