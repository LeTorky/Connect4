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
            while ((Connection != null) && (Connection.ClientSocket.Poll(1,SelectMode.SelectRead) != true) || (Connection.ClientSocket.Available != 0))
            {
                EncodedRequest = new byte[1000];
                Connection.ClientStream.Read(EncodedRequest, 0, EncodedRequest.Length);
                Connection.ClientStream.Flush();
                lock (HostingRemoteEnds) lock(SocketPairList) lock(Connection.ClientName)
                {
                    if (Encoding.ASCII.GetString(EncodedRequest).Contains("Host:"))
                    {
                        if (HostingRemoteEnds.Contains(Connection) == false)
                        {
                            string[] HostString = Encoding.ASCII.GetString(EncodedRequest).Trim((char)0).Split(new string[] { "Host:" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                            Connection.ClientName = HostString[0];
                            Connection.TokenColor = HostString[1] == "LightSeaGreen" ? Color.LightSeaGreen : Color.FromArgb(252, 175, 23);
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
                    else if(Encoding.ASCII.GetString(EncodedRequest).Contains("EndHost") && HostingRemoteEnds.Contains(Connection))
                    {
                        Console.WriteLine($"Host Ended: {Connection.ClientSocket.RemoteEndPoint}");
                        HostingRemoteEnds.Remove(Connection);
                    }
                    else if(Encoding.ASCII.GetString(EncodedRequest).Contains("Score:") && HostingRemoteEnds.Contains(Connection))
                    {
                        string[] DecodedMessage = new string[0];
                        DecodedMessage = Encoding.ASCII.GetString(EncodedRequest).Trim((char)0).Split(new string[] { "Score:" }, StringSplitOptions.RemoveEmptyEntries)[0].Split('*');
                        string filePath = Path.GetFullPath("Score.txt");
                        string FileContent = File.ReadAllText("E:\\Codes\\Visual C#\\Project\\Connect4\\Server\\Score.txt");
                        string ScoreStringOne = $"PlayerOne:{DecodedMessage[0].Split(',')[0]},PlayerTwo:{DecodedMessage[0].Split(',')[1]},Score:";
                        string ScoreStringTwo = $"PlayerOne:{DecodedMessage[0].Split(',')[1]},PlayerTwo:{DecodedMessage[0].Split(',')[0]},Score:";
                        int ScoreIndexOne = FileContent.IndexOf(ScoreStringOne);
                        int ScoreIndexTwo = FileContent.IndexOf(ScoreStringTwo);
                        int SemiColumnIndex;
                        if (ScoreIndexOne!= -1)
                        {
                            SemiColumnIndex = FileContent.IndexOf(';', ScoreIndexOne + ScoreStringOne.Length);
                            string[] ScoreArray = FileContent.Substring(ScoreIndexOne + ScoreStringOne.Length, SemiColumnIndex - ScoreIndexOne - ScoreStringOne.Length).Split(',');
                            int ScoreOne = int.Parse(ScoreArray[0]);
                            int ScoreTwo = int.Parse(ScoreArray[1]);
                            Console.WriteLine($"{ScoreArray[0]},{ScoreArray[1]}");
                            int ScoreOneIndex = FileContent.IndexOf(ScoreArray[0], ScoreIndexOne + ScoreStringOne.Length);
                            int CommaIndex = FileContent.IndexOf(',', ScoreOneIndex);
                            int ScoreTwoIndex = FileContent.IndexOf(ScoreArray[1], CommaIndex);
                            switch (DecodedMessage[1])
                            {
                                case "1":
                                    ScoreOne++;
                                    FileContent = FileContent.Remove(ScoreOneIndex, ScoreArray[0].Length).Insert(ScoreOneIndex, ScoreOne.ToString());
                                    break;
                                case "2":
                                Console.WriteLine("2");
                                    ScoreTwo++;
                                    FileContent = FileContent.Remove(ScoreTwoIndex, ScoreArray[1].Length).Insert(ScoreTwoIndex, ScoreTwo.ToString());
                                    break;
                            }
                            File.WriteAllText("E:\\Codes\\Visual C#\\Project\\Connect4\\Server\\Score.txt", FileContent);
                            ScoreIndexTwo = FileContent.IndexOf($"PlayerOne:{DecodedMessage[0].Split(',')[1]},PlayerTwo:{DecodedMessage[0].Split(',')[0]},Score:");
                        }
                        if (ScoreIndexTwo!= -1)
                        {
                            SemiColumnIndex = FileContent.IndexOf(';', ScoreIndexTwo + ScoreStringTwo.Length);
                            string[] ScoreArray = FileContent.Substring(ScoreIndexTwo + ScoreStringTwo.Length, SemiColumnIndex - ScoreIndexTwo - ScoreStringTwo.Length).Split(',');
                            int ScoreOne = int.Parse(ScoreArray[0]);
                            int ScoreTwo = int.Parse(ScoreArray[1]);
                            int ScoreOneIndex = FileContent.IndexOf(ScoreArray[0], ScoreIndexTwo + ScoreStringTwo.Length);
                            int ScoreTwoIndex = FileContent.IndexOf(ScoreArray[1], ScoreIndexTwo + ScoreStringTwo.Length);
                            switch (DecodedMessage[1])
                            {
                                case "1":
                                    ScoreOne++;
                                    FileContent = FileContent.Remove(ScoreOneIndex, ScoreArray[0].Length).Insert(ScoreOneIndex, ScoreOne.ToString());
                                    break;
                                case "2":
                                    ScoreTwo++;
                                    FileContent = FileContent.Remove(ScoreTwoIndex, ScoreArray[1].Length).Insert(ScoreTwoIndex, ScoreTwo.ToString());
                                    break;
                            }
                            File.WriteAllText("E:\\Codes\\Visual C#\\Project\\Connect4\\Server\\Score.txt", FileContent);
                        }
                        if(ScoreIndexTwo == -1 && ScoreIndexOne == -1)
                        {
                            switch (DecodedMessage[1])
                            {
                                case "1":
                                    File.AppendAllText("E:\\Codes\\Visual C#\\Project\\Connect4\\Server\\Score.txt",
                                    $"\nPlayerOne:{DecodedMessage[0].Split(',')[0]},PlayerTwo:{DecodedMessage[0].Split(',')[1]},Score:1,0;");
                                    break;
                                case "2":
                                    File.AppendAllText("E:\\Codes\\Visual C#\\Project\\Connect4\\Server\\Score.txt",
                                    $"\nPlayerOne:{DecodedMessage[0].Split(',')[0]},PlayerTwo:{DecodedMessage[0].Split(',')[1]},Score:0,1;");
                                    break;
                            }
                        }
                    }
                }
            }
        }
        /*---------------------------------------End of Methods-------------------------------------------*/
    }
    /*------------------------------------------End of Program Class--------------------------------------*/
}
 