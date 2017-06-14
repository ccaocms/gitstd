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
                    Console.WriteLine("listenclientconnect end");
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
            Console.WriteLine("listenclientconnect end");
        }


        private static void ReceiveMessage(object clientSocket)
        {
            int receiveNumber=0;

            client = (Socket)clientSocket;
       
            while (reciveblock==true)
            {

                if (client != null && client.Connected)//only do when client is available and connected
                {
                    
                    Console.WriteLine("client is not null");
                    client.ReceiveTimeout = 10000;//10seconds receive timeout
                    //receive data added receive timeout, if exception happend continue
                    try
                    {
                        receiveNumber = client.Receive(result);//获取接收数据的长度
                    }
                    catch (Exception e)
                    {
                        /*any kind of exception will causing clinet disconnect and this thread been stopped*/
                        Console.WriteLine(e.Message + "\r\nshould not be OK\r\n");
                        ReleaseSocket(client);
                        Console.WriteLine("*********************\r\nreceivemsg end\r\n********************");
                        break;//stop the thread 
                        
                    }

                    Console.WriteLine("Socket Server get info: " + System.Text.Encoding.Default.GetString(result));

                    
                    if (receiveNumber > 0)//during the thread release, the receive api still returns so adding logic here to prevent exceptions
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
            Console.WriteLine("*********************\r\nreceivemsg end\r\n********************");
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
                if (client != null&&client.Connected)//wait until receive timeout to let client disconnect
                {
                    Thread.Sleep(11000);
                }
                
                if (ClientCheckingTimer != null)//stop check timer
                {
                    ClientCheckingTimer.Stop();
                    ClientCheckingTimer.Close();
                }
                if (receiveThread!=null)//stop all thread
                {
                    reciveblock = false;
                    receiveThread.Abort();
                    client = null;
                    threadblock = false;

                    
                }
                
                try
                {
                    server.Close();//close server, this will trigger server.accept() exception and it is the only way to close server right now
                }
                catch (Exception e)
                {
                     Console.WriteLine("commer server close error" + e.Message);
                }

            }

        }

    }

}
