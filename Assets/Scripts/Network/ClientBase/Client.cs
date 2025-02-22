﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ClientBase
{
    public class Client
    {
        public const int MAX_BUFFER_SIZE = 128 * 1024;
        private static Client instance;
        public bool isConnect = false;
        public Socket client;

        private ProtocolBytes proto = new ProtocolBytes();

        private int start = 0;
        private int length = 0;

        public int port = -1;

        private byte[] buffer = new byte[MAX_BUFFER_SIZE];

        public PlayerInfo pl_info = new PlayerInfo();

        private string host;
        public string Host
        {
            get
            {
                return host;
            }
            set
            {
                host = value;
            }
        }

        public static Client Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                else
                {
                    instance = new Client();
                    return instance;
                }
            }
        }
        private Client()
        {

        }
        public void Connect()
        {
            if (host == null)
                return;
            if (client != null)
            {
                Console.WriteLine("There is a connection, reconnect?");
            }
            if (client != null && client.Connected)
            {
                Console.WriteLine("Already connect");
            }
            if (port < 0)
                return;
            try
            {
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint iPEndPoint = new IPEndPoint(ip, port);
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(iPEndPoint);
                client.BeginReceive(buffer, start, MAX_BUFFER_SIZE - start,
                    SocketFlags.None, ReceiveCallback, null);
                //isConnect = client.Connected;
                isConnect = true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log(e.StackTrace);
            }
            if (client.Connected)
                Debug.Log("It seems that the connection has been built.");
        }

        public bool Connect(string _host, string _port)
        {
            try
            {
                if (int.TryParse(_port, out port))
                {
                    this.host = _host;
                    Connect();
                    return client.Connected;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Invoke this when accept message from client.
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                start += client.EndReceive(ar);

                DataProcessor();

                client.BeginReceive(buffer, start, MAX_BUFFER_SIZE - start,
SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception e)
            {
                isConnect = false;
                Console.WriteLine(e);
            }
        }

        byte[] lenBytes = new byte[sizeof(short)];
        /// <summary>
        /// 处理信息，管理接收到的数据
        /// </summary>
        private void DataProcessor()
        {
            //isConnect = client.Connected;
            //如果小于存储长度的数据长度，则返回
            if (start < sizeof(short))
                return;
            length = BitConverter.ToInt16(buffer, 0);

            //如果没接收完毕返回
            if (start < SF.SHORT_SIZE + length)
                return;

            ProtocolBase protocol = proto.Decode(buffer, SF.SHORT_SIZE, length);

            //Debug.Log();

            //todo this is to deal with protocol
            bool cor = false;
            //length
            if (length >= 144)
            {
                CRC16 crc16 = new CRC16(protocol.Encode(), true);
                crc16.CRC_16();
                cor = crc16.IsCorrect();
            }
            else
            {
                CRC8 crc8 = new CRC8(protocol.Encode(), CRC_op.Judge);
                //Debug.Log("CRC8");
                cor = crc8.IsCorrect();
            }

            if (cor)
            {
                //Add handler and handle.
                //Debug.Log(protocol.GetName() + " is cor");
                EventHandler.GetEventHandler().AddProtocol(protocol);
            }
            else
            {
                //Debug.Log("CRC failed");
            }

            //Operations for protocol
            int count = start - SF.SHORT_SIZE - length;
            Array.Copy(buffer, length + SF.SHORT_SIZE, buffer, 0, count);
            //Debug.Log(string.Format("start = {0}, count = {1}, length = {2}", start, count, length));
            start = count;
            if (count > 0)
                DataProcessor();
            return;
        }


        /// <summary>
        /// 
        /// </summary>
        public void Reconnect()
        {
            if (isConnect)
            {
                return;
            }
            if (client == null)
            {
                return;
            }
            Connect();
        }

        public void Disconnect()
        {
            if (!isConnect)
            {
                return;
            }
            try
            {
                client.Disconnect(true);
                isConnect = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Send msg to server
        /// </summary>
        public void Send(ProtocolBase protocol)
        {
            if (!isConnect)
            {
                Debug.Log("Try to send but failed : isConnect = false");
                //return;
            }
            try
            {
                //if (ClientLauncher.Instance.EnableCrc)
                    protocol.AppendCrc();
                //把传输的信息转化为字节数组A
                byte[] bytes = protocol.Encode();
                //把信息长度大小转换成字节数组
                byte[] length = BitConverter.GetBytes((short)bytes.Length);
                //这段话表示连接length和bytes数组，并且length在前
                byte[] sendBuff = length.Concat(bytes).ToArray();
                client.Send(sendBuff);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log(e.StackTrace);
            }
        }
    }
}
