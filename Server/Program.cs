using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using ConnectionClasses;
using System.Drawing;
namespace Server
{
    /*---------------------------------------------Program Class-------------------------------------------*/
    class Program
    {
        /*---------------------------------------------Fields-----------------------------------------------*/
        //Server Fields
        static IPAddress ServerIP;
        static int ServerPort;
        static TcpListener ServerTCP;
        static List<ClientConnection> SocketPairList;
        static List<ClientConnection> HostingRemoteEnds;
        static int SleepingTime;

        //Threads
        static Thread ConnectionThread;
        static Thread CheckAndUpdate;
        /*------------------------------------------End of Fields-------------------------------------------*/


        /*-----------------------------------------------Main-----------------------------------------------*/
        static void Main(string[] args)
        {
            //Construct Lists
            SocketPairList = new List<ClientConnection>();
            HostingRemoteEnds = new List<ClientConnection>();
            //Get IP Address (To be changed into External after Port Forwarding).
            ServerIP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
            ServerPort = 5500;
            //Output Servers IP and Port.
            Console.WriteLine($"Server IP: {ServerIP}, Server Port: {ServerPort}");
            //Init TCP Listener with IP and Port.
            ServerTCP = new TcpListener(ServerIP, ServerPort);
            //Starts Server.
            ServerTCP.Start();
            //Construct Connection Thread with Listening Method.
            ConnectionThread = new Thread(ListenToConnections);
            CheckAndUpdate = new Thread(CheckForConnections);
            //Sleeping Time
            SleepingTime = 3000;
            //Starts Threads.
            ConnectionThread.Start();
            //CheckAndUpdate.IsBackground = true;
            CheckAndUpdate.Start();
        }
        /*-------------------------------------------End of Main-------------------------------------------*/


        /*-------------------------------------------Methods----------------------------------------------*/

        /*---Listens for New Connections---*/
        static void ListenToConnections()
        {
            //Listening for new connections and adding to the Network Stream and SocketPair Lists.
            Socket InsertSocket;
            while (true)
            {
                InsertSocket = ServerTCP.AcceptSocket();
                lock (SocketPairList)
                {
                    SocketPairList.Add(new ClientConnection(InsertSocket));
                    Console.WriteLine($"New Connection: {SocketPairList.Last<ClientConnection>().ClientSocket.RemoteEndPoint}");
                    ThreadPool.QueueUserWorkItem(ReadHostRequest, SocketPairList.Last<ClientConnection>());
                }
            }
        }

        /*---Check for Connections and Update Connected---*/
        static void CheckForConnections()
        {
            //Checking Disconnections to remove from Network Stream and SocketPair Lists, else update with Rooms Information.
            byte[] EncodedRooms;
            Stack<int> RemoveSockets = new Stack<int>();
            string RoomInformation;
            while (true)
            {
                RemoveSockets.Clear();
                lock (SocketPairList) lock(HostingRemoteEnds)
                { 
                    foreach(ClientConnection SpecificConnection in SocketPairList)
                    {
                        lock (SpecificConnection.ClientName)
                        { 
                            RoomInformation = "";
                            if (SpecificConnection.ClientSocket.Poll(1,SelectMode.SelectRead) && (SpecificConnection.ClientSocket.Available == 0))
                            {
                                RemoveSockets.Push(SocketPairList.IndexOf(SpecificConnection));
                                Console.WriteLine($"Disconnected: {SpecificConnection.ClientSocket.RemoteEndPoint}");
                            }
                            else
                            {
                                foreach(ClientConnection HostConnection in HostingRemoteEnds)
                                {
                                    RoomInformation += (HostConnection.ClientSocket.RemoteEndPoint.ToString() + "," + HostConnection.ClientName.Trim((char)0) + "," + HostConnection.Status.ToString() + "," + HostConnection.PlayerCount.ToString() + "," + HostConnection.TokenColor.ToString() + "," + HostConnection.BoardSize.ToString() + ";");
                                }
                                RoomInformation = RoomInformation == "" ? "Empty" : RoomInformation;
                                EncodedRooms = Encoding.ASCII.GetBytes(RoomInformation);
                                SpecificConnection.ClientStream.Write(EncodedRooms, 0, EncodedRooms.Length);
                            }
                        }
                    }
                    foreach(int i in RemoveSockets)
                    {
                        HostingRemoteEnds.Remove(SocketPairList[i]);
                        SocketPairList[i].ClientStream.Close();
                        SocketPairList[i].ClientSocket.Close();
                        SocketPairList.RemoveAt(i);
                    }
                }
                //Wait time to reinvoke thread
                Thread.Sleep(SleepingTime);
            }
        }

        /*---Listen for Hosting Requests---*/
        static void ReadHostRequest(object SocketStreamPair)
        {
            ClientConnection Connection = (ClientConnection)SocketStreamPair;
            byte[] EncodedRequest;
            while ((Connection.ClientSocket.Poll(1,SelectMode.SelectRead) != true) || (Connection.ClientSocket.Available != 0))
            {
                EncodedRequest = new byte[1000];
                try
                {
                    Connection.ClientStream.Read(EncodedRequest, 0, EncodedRequest.Length);
                }
                catch(Exception Obj)
                {
                   //When Connection Cuts Off While Reading for a Host Command.
                }
                Connection.ClientStream.Flush();
                lock (HostingRemoteEnds) lock(SocketPairList) lock(Connection.ClientName)
                {
                    if (Encoding.ASCII.GetString(EncodedRequest).Contains("Host"))
                    {
                        if (HostingRemoteEnds.Contains(Connection) == false)
                        {
                            string[] HostString = Encoding.ASCII.GetString(EncodedRequest).Trim((char)0).Split(new string[] { "Host:" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                            Console.WriteLine(Encoding.ASCII.GetString(EncodedRequest).Trim((char)0));
                            Connection.ClientName = HostString[0];
                            Connection.TokenColor = HostString[1] == "Green" ? Color.Green : Color.Orange;
                            Connection.BoardSize = HostString[2];
                            HostingRemoteEnds.Add(Connection);
                            Console.WriteLine($"Host: {Connection.ClientSocket.RemoteEndPoint}");
                        }
                    }
                    else if (Encoding.ASCII.GetString(EncodedRequest).Contains("Info") && HostingRemoteEnds.Contains(Connection))
                    {
                        string FilteredString = Encoding.ASCII.GetString(EncodedRequest).Trim((char)0).Split(new string[] { "Info:" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        Connection.PlayerCount = int.Parse(FilteredString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0]);
                        if(FilteredString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)[1] == "started")
                        {
                            Connection.Status = GameStatus.started;
                        }
                        else
                        {
                            Connection.Status = GameStatus.waiting;
                        }
                    }
                }
            }
        }
        /*---------------------------------------End of Methods-------------------------------------------*/
    }
    /*------------------------------------------End of Program Class--------------------------------------*/
}
 