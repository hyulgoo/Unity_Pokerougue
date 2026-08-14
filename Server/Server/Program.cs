using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Game.Room;
using ServerCore;
using SharedDB;
using Timer = System.Timers.Timer;

namespace Server
{
    // 1. GameRoom 방식의 간단한 동기화 <- OK
    // 2. 더 넓은 영역 관리
    // 3. 심리스 MMO

    // 1. Recv (N개)     서빙
    // 2. GameLogic (1)  요리사
    // 3. Send (1개)     서빙
    // 4. DB (1)         결제/장부

    internal class Program
    {
        private static readonly Listener _listener = new Listener();

        public static string Name { get; } = "Server";
        public static int Port { get; } = 7777;
        public static string IpAddress { get; set; }

        private static void GameLogicTask()
        {
            while (true)
            {
                GameLogic.Instance.Update();
                Thread.Sleep(0);
            }
        }

        private static void DbTask()
        {
            while (true)
            {
                DbTransaction.Instance.Flush();
                Thread.Sleep(0);
            }
        }

        private static void NetworkTask()
        {
            while (true)
            {
                List<ClientSession> sessions = SessionManager.Instance.GetSessions();
                foreach (ClientSession session in sessions) session.FlushSend();

                Thread.Sleep(0);
            }
        }

        private static void StartServerInfoTask()
        {
            Timer t = new Timer();
            t.AutoReset = true;
            t.Elapsed += (s, e) =>
            {
                using (SharedDbContext shared = new SharedDbContext())
                {
                    ServerDb serverDb = shared.Servers.Where(s => s.Name == Name).FirstOrDefault();
                    if (serverDb != null)
                    {
                        serverDb.IpAddress = IpAddress;
                        serverDb.Port = Port;
                        serverDb.BusyScore = SessionManager.Instance.GetBusyScore();
                        shared.SaveChangesEx();
                    }
                    else
                    {
                        serverDb = new ServerDb
                        {
                            Name = Name,
                            IpAddress = IpAddress,
                            Port = Port,
                            BusyScore = SessionManager.Instance.GetBusyScore()
                        };
                        shared.Servers.Add(serverDb);
                        shared.SaveChangesEx();
                    }
                }
            };
            t.Interval = 10 * 1000;
            t.Start();
        }

        private static void Main(string[] args)
        {
            ConfigManager.LoadConfig();
            DataManager.LoadData();

            GameLogic.Instance.Push(() => { GameLogic.Instance.Add(); });

            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, Port);

            IpAddress = ipAddr.ToString();

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");

            StartServerInfoTask();

            // DbTask
            {
                Thread t = new Thread(DbTask);
                t.Name = "DB";
                t.Start();
            }

            // NetworkTask
            {
                Thread t = new Thread(NetworkTask);
                t.Name = "Network Send";
                t.Start();
            }

            // GameLogic
            Thread.CurrentThread.Name = "GameLogic";
            GameLogicTask();
        }
    }
}