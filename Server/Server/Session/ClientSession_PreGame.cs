using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Game;
using Server.Game.Room;
using ServerCore;
using SharedDB;
using GameRoom = Server.Game.Room.GameRoom;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        public ClientSession(Socket socket, long lastSendTick, long pingpongTick, int reservedSendBytes, PlayerServerState serverState, Player myPlayer, int sessionId, int accountDbId) : base(socket)
        {
            _lastSendTick = lastSendTick;
            _pingpongTick = pingpongTick;
            _reservedSendBytes = reservedSendBytes;
            ServerState = serverState;
            MyPlayer = myPlayer;
            SessionId = sessionId;
            AccountDbId = accountDbId;
        }

        private int AccountDbId { get; set; }
        private List<LobbyPlayerInfo> LobbyPlayers { get; } = new List<LobbyPlayerInfo>();

        // 토큰이 발급돼 있고, 값이 같고, 만료 전이어야 한다.
        internal static bool IsLoginTokenValid(TokenDb tokenDb, int token, DateTime utcNow)
        {
            return tokenDb != null && tokenDb.Token == token && tokenDb.Expired > utcNow;
        }

        // AccountServer가 로그인 때 SharedDB에 적어 둔 토큰과 대조한다.
        private static bool VerifyLoginToken(int accountId, int token)
        {
            using SharedDbContext shared = new SharedDbContext();
            TokenDb tokenDb = shared.Tokens.AsNoTracking().FirstOrDefault(t => t.AccountDbId == accountId);
            return IsLoginTokenValid(tokenDb, token, DateTime.UtcNow);
        }

        public void HandleLogin(C_Login loginPacket)
        {
            // TODO : 이런 저런 보안 체크
            if (ServerState != PlayerServerState.ServerStateLogin)
                return;

            // 토큰이 맞지 않으면 접속을 끊는다. 한 접속에서 로그인은 한 번만 시도할 수 있다.
            if (!VerifyLoginToken(loginPacket.AccountId, loginPacket.Token))
            {
                Disconnect();
                return;
            }

            // TODO : 문제가 있긴 있다
            // - 동시에 다른 사람이 같은 UniqueId을 보낸다면?
            // - 악의적으로 여러번 보낸다면
            // - 쌩뚱맞은 타이밍에 그냥 이 패킷을 보낸다면?

            LobbyPlayers.Clear();

            // 게임 계정은 검증된 계정 ID로 찾는다. 클라이언트가 보낸 UniqueId는 쓰지 않는다.
            string accountName = loginPacket.AccountId.ToString();

            using AppDbContext db = new AppDbContext();
            AccountDb findAccount = db.Accounts
                .Include(a => a.Players).FirstOrDefault(a => a.AccountName == accountName);

            if (findAccount != null)
            {
                // AccountDbId 메모리에 기억
                AccountDbId = findAccount.AccountDbId;

                S_Login loginOk = new S_Login { LoginOk = 1 };
                foreach (PlayerDb playerDb in findAccount.Players)
                {
                    LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo
                    {
                        PlayerDbId = playerDb.PlayerDbId,
                        Name = playerDb.PlayerName
                    };

                    // 메모리에도 들고 있다
                    LobbyPlayers.Add(lobbyPlayer);

                    // 패킷에 넣어준다
                    loginOk.Players.Add(lobbyPlayer);
                }

                Send(loginOk);
            }
            else
            {
                AccountDb newAccount = new AccountDb { AccountName = accountName };
                db.Accounts.Add(newAccount);
                bool success = db.SaveChangesEx();
                if (!success)
                    return;

                // AccountDbId 메모리에 기억
                AccountDbId = newAccount.AccountDbId;

                S_Login loginOk = new S_Login { LoginOk = 1 };
                Send(loginOk);
            }

            // 로비로 이동
            ServerState = PlayerServerState.ServerStateLobby;
        }

        public void HandleEnterGame(C_EnterGame enterGamePacket)
        {
            if (ServerState != PlayerServerState.ServerStateLobby)
                return;

            LobbyPlayerInfo playerInfo = LobbyPlayers.Find(p => p.Name == enterGamePacket.Name);
            if (playerInfo == null)
                return;

            //MyPlayer = ObjectManager.Instance.Add<Player>();
            //{
            //	MyPlayer.PlayerDbId = playerInfo.PlayerDbId;
            //	MyPlayer.Info.Name = playerInfo.Name;
            //	MyPlayer.Session = this;

            //	S_ItemList itemListPacket = new S_ItemList();

            //	// 아이템 목록을 갖고 온다
            //	using (AppDbContext db = new AppDbContext())
            //	{
            //		List<ItemDb> items = db.Items
            //			.Where(i => i.OwnerDbId == playerInfo.PlayerDbId)
            //			.ToList();

            //		foreach (ItemDb itemDb in items)
            //		{
            //			Item item = Item.MakeItem(itemDb);
            //			if (item != null)
            //			{
            //				MyPlayer.Inven.Add(item);

            //				ItemInfo info = new ItemInfo();
            //				info.MergeFrom(item.Info);
            //				itemListPacket.Items.Add(info);
            //			}
            //		}
            //	}

            //	Send(itemListPacket);
            //}

            GameLogic.Instance.Push(() =>
            {
                GameRoom room = GameLogic.Instance.Find(1);
                room.Push(room.SetPlayerBySession, this, playerInfo);
            });
        }

        public void HandleCreatePlayer(C_CreatePlayer createPacket)
        {
            // TODO : 이런 저런 보안 체크
            if (ServerState != PlayerServerState.ServerStateLobby)
                return;

            using AppDbContext db = new AppDbContext();
            PlayerDb findPlayer = db.Players.FirstOrDefault(p => p.PlayerName == createPacket.Name);

            if (findPlayer != null)
            {
                // 이름이 겹친다
                Send(new S_CreatePlayer());
            }
            else
            {
                //// 1레벨 스탯 정보 추출
                //StatInfo stat = null;
                //DataManager.StatDict.TryGetValue(1, out stat);

                // DB에 플레이어 만들어줘야 함
                PlayerDb newPlayerDb = new PlayerDb
                {
                    PlayerName = createPacket.Name,
                    AccountDbId = AccountDbId
                };

                db.Players.Add(newPlayerDb);
                bool success = db.SaveChangesEx();
                if (!success)
                    return;

                // 메모리에 추가
                LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo
                {
                    PlayerDbId = newPlayerDb.PlayerDbId,
                    Name = createPacket.Name
                };

                // 메모리에도 들고 있다
                LobbyPlayers.Add(lobbyPlayer);

                // 클라에 전송
                S_CreatePlayer newPlayer = new S_CreatePlayer { Player = new LobbyPlayerInfo() };
                newPlayer.Player.MergeFrom(lobbyPlayer);

                Send(newPlayer);
            }
        }

        public void HandleReEnterHandler(int roomId)
        {
            S_Login loginOk = new S_Login { LoginOk = 1 };
            foreach (LobbyPlayerInfo lobbyPlayer in LobbyPlayers)
                loginOk.Players.Add(lobbyPlayer);

            Send(loginOk);

            ServerState = PlayerServerState.ServerStateLobby;
        }
    }
}