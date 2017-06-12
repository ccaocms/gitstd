using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace CMSTIP
{
    public class Common_SocketServer
    {
        

        private int port;
        private static byte[] result = new byte[1024];
        private static Socket server;
        private IPAddress ip;
        private static Socket client;
        private static Thread myThread;
        private static Thread receiveThread;
        //private static int ConnectionCount = 0;
        private static System.Timers.Timer ClientCheckingTimer;

        private static List<Socket> AllSockets = new List<Socket>();
        private static Object[,] SocketList = new Object[2, 2];

        private static bool threadblock;//stop thread;
        private static bool reciveblock;//stop thread;

        public Common_SocketServer(string ipadr, int port)
        {
            this.port = port;
            ip = IPAddress.Parse(ipadr);
        }
        public Boolean setConnection()
        {
            try
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(ip, port));
                server.Listen(1);
                threadblock = true;
                reciveblock = true;
                myThread = new Thread(ListenClientConnect);
                myThread.Start();
                //ListenClientConnect();

                Console.WriteLine("Begin listening in port " + port);
            }
            catch (Exception e)
            {
                Console.WriteLine("start socket server get exception: " + e.Message);
                return false;
            }

            ClientCheckingTimer = new System.Timers.Timer(5000);  // 5000 milliseconds = 5 seconds
            ClientCheckingTimer.AutoReset = true;
            ClientCheckingTimer.Elapsed += new System.Timers.ElapsedEventHandler(ClientCheckingTimeout);
            ClientCheckingTimer.Start();

            return true;
        }

        private static void ClientCheckingTimeout(object sender, System.Timers.ElapsedEventArgs e)
        {


            Console.WriteLine("begin checking clients, allsockets count: " + AllSockets.Count());

            for (int i = 0; i < AllSockets.Count(); i++)
            {
                if (AllSockets[i] == null)
                {
                    Console.WriteLine("client is null");

                    ReleaseSocket(AllSockets[i]);
                }
                else
                {
                    Console.WriteLine("client not null, begin process");
                    Boolean socketstatus = true;
                    socketstatus = IsConnected(AllSockets[i]);

                    Console.WriteLine("timeout check socket status: " + socketstatus);

                    if (!socketstatus)
                    {
                        Console.WriteLine("socket is not avaliable, begin to release");


                        ReleaseSocket(AllSockets[i]);

                    }

                }
            }
        }

        private static void ReleaseSocket(Socket socket)
        {
            Console.WriteLine("begin socket release processing");

            try
            {
                receiveThread.Abort();
            }
            catch (Exception ex)
            {
                 Console.WriteLine("Thread abort get exception: " + ex.Message);
            }
            AllSockets.Remove(socket);

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                 Console.WriteLine("socket shutdown get exception: " + ex.Message);
            }

            try
            {
                socket.Close();
            }
            catch (Exception ex)
            {
                 Console.WriteLine("socket close get exception: " + ex.Message);
            }

        }

        public static bool IsConnected(Socket socket)
        {

            try
            {
                bool part1 = socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (socket.Available == 0);
                if (part1 & part2)
                {//connection is closed
                    return false;
                }

            }
            catch (Exception ex)
            {
                 Console.WriteLine("IsConnected get excetpion: " + ex.Message);
                return false;
            }
            return true;
        }

        private static void ListenClientConnect()
        {
            
            //Boolean Listening = true;
            while (threadblock== true)
            {
                try
                {
                    client = server.Accept();
                    AllSockets.Add(client);
                }
                catch {
                    Console.WriteLine("server accept() error");
                    return;
                }
                
               

                //Console.WriteLine("AllSockets count is: " + AllSockets.Count());
                //do checking first before begin receving data

                if (AllSockets.Count() > 2)
                {
                    //if there's too many connection, disconnect the last one
                    SendMsg(client, "Too many connections");
                    AllSockets.Remove(client);

                    client.Close();
                     Console.WriteLine("After disconnect client, now allsockets count is: " + AllSockets.Count());
                    //Listening = false;
                }
                else
                {
                    receiveThread = new Thread(ReceiveMessage);
                    receiveThread.Start(client);
                }
                Thread.Sleep(1000);
            }
        }


        private static void ReceiveMessage(object clientSocket)
        {
            int receiveNumber=0;

            client = (Socket)clientSocket;
       
            while (reciveblock==true)
            {

                if (client != null && client.Connected)
                {
                    
                    Console.WriteLine("client is not null");
                    //receive data added receive timeout, if exception happend continue
                    try
                    {
                        receiveNumber = client.Receive(result);//获取接收数据的长度
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\r\nshould be OK\r\n");
                        continue;
                    }

                    Console.WriteLine("Socket Server get info: " + System.Text.Encoding.Default.GetString(result));

                    
                    if (receiveNumber > 0)
                    {
                        SendMsg(clientSocket, "Hello from Server");
                    }
                    
                }
                else
                {
                    Console.WriteLine("client is  null!!!!!!");
                    reciveblock = false;
                }
                   
               
            }
        }

        private static void SendMsg(object clientSocket, String msg)
        {
            client = (Socket)clientSocket;

            try
            {
                client.Send(Encoding.ASCII.GetBytes(msg));
            }
            catch (Exception e)
            {
                 Console.WriteLine("Sending message to socket client get exception: " + e.Message);
            }
        }

        public static void ReleaseServer()
        {
            if (server != null)
            {
                try
                {
                    if (client!=null)
                    {
                        client.Shutdown(SocketShutdown.Both);
                        Console.WriteLine("commer server shutdown");
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("commer server shutdown" + e.Message);
                }
                if (ClientCheckingTimer != null)
                {
                    ClientCheckingTimer.Stop();
                    ClientCheckingTimer.Close();
                }
                if (receiveThread!=null)
                {
                    reciveblock = false;
                    Thread.Sleep(6000);
                    receiveThread.Abort();
                    client = null;
                    threadblock = false;

                    Thread.Sleep(6000);
                }
                
                 // myThread.Abort();
                try
                {
                    server.Close();
                }
                catch (Exception e)
                {
                     Console.WriteLine("commer server close error" + e.Message);
                }

            }

        }

    }

}
