using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using Server.Game.Room;
using Server.Packet;
using GameRoom = Server.Game.Room.GameRoom;

namespace Server
{
    public partial class ClientSession
    {
        private readonly object _lock = new object();
        private long _lastSendTick;

        private long _pingpongTick;
        private List<ArraySegment<byte>> _reserveQueue = new List<ArraySegment<byte>>();

        // 패킷 모아 보내기
        private int _reservedSendBytes;

        public ClientSession() : base(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            _lastSendTick = Environment.TickCount64;
            _pingpongTick = 0;
            _reservedSendBytes = 0;
            ServerState = PlayerServerState.ServerStateLogin;
            MyPlayer = null;
            SessionId = 0;
        }

        public PlayerServerState ServerState { get; private set; } = PlayerServerState.ServerStateLogin;

        public Player MyPlayer { get; set; }
        public int SessionId { get; set; }

        private void Ping()
        {
            if (_pingpongTick > 0)
            {
                long delta = Environment.TickCount64 - _pingpongTick;
                if (delta > 30 * 1000)
                {
                    Console.WriteLine("Disconnected by PingCheck");
                    Disconnect();
                    return;
                }
            }

            S_Ping pingPacket = new S_Ping();
            Send(pingPacket);

            GameLogic.Instance.PushAfter(5000, Ping);
        }

        public void HandlePong()
        {
            _pingpongTick = Environment.TickCount64;
        }

        public void HandleRespondDuel(S_RespondDuel packet, int roomId)
        {
            if (packet.DuelOK == 1)
            {
                Player player = MyPlayer;
                GameRoom room = player.Room;

                ServerState = PlayerServerState.ServerStateGame;
                room.Push(room.LeaveGame, player.Info.ObjectId);

                MyPlayer = player;
                room = GameLogic.Instance.Find(roomId);
                ClientSession session = player.Session;

                LobbyPlayerInfo info = session.LobbyPlayers.Find(p => p.Name == player.Info.Name);

                room.Push(room.SetPlayerBySession, session, info);
            }

            Send(packet);
        }

        #region Network

        // 예약만 하고 보내지는 않는다
        public void Send(IMessage packet)
        {
            string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
            MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
            ushort size = (ushort)packet.CalculateSize();
            byte[] sendBuffer = new byte[size + 4];
            Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

            lock (_lock)
            {
                _reserveQueue.Add(sendBuffer);
                _reservedSendBytes += sendBuffer.Length;
            }
        }

        // 실제 Network IO 보내는 부분
        public void FlushSend()
        {
            List<ArraySegment<byte>> sendList;

            lock (_lock)
            {
                // 0.1초가 지났거나, 너무 패킷이 많이 모일 때 (1만 바이트)
                long delta = Environment.TickCount64 - _lastSendTick;
                if (delta < 100 && _reservedSendBytes < 10000)
                    return;

                // 패킷 모아 보내기
                _reservedSendBytes = 0;
                _lastSendTick = Environment.TickCount64;

                sendList = _reserveQueue;
                _reserveQueue = new List<ArraySegment<byte>>();
            }

            Send(sendList);
        }

        public override void OnConnected(EndPoint endPoint)
        {
            //Console.WriteLine($"OnConnected : {endPoint}");

            {
                S_Connected connectedPacket = new S_Connected();
                Send(connectedPacket);
            }

            GameLogic.Instance.PushAfter(5000, Ping);
        }

        protected override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            GameLogic.Instance.Push(() =>
            {
                if (MyPlayer == null)
                    return;

                GameRoom room = GameLogic.Instance.Find(MyPlayer.Room.RoomId);
                room.Push(room.LeaveGame, MyPlayer.Info.ObjectId);
            });

            SessionManager.Instance.Remove(this);
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }

        #endregion
    }
}