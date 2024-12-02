using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
	{
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();

		Dictionary<int, bool> _playerReady = new Dictionary<int, bool>();

		public void Init()
		{			
		}

		// 누군가 주기적으로 호출해줘야 한다
		public void Update()
		{
			Flush();
		}

		Random _rand = new Random();
		public void EnterGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

			if (type == GameObjectType.Player)
			{
				Player player = gameObject as Player;
				_players.Add(gameObject.Id, player);
				_playerReady.Add(gameObject.Id, false);
				player.Room = this;

				// 본인한테 정보 전송
				S_EnterGame enterPacket = new S_EnterGame();
				enterPacket.Player = player.Info;
				player.Session.Send(enterPacket);
			}

			// 타인한테 정보 전송
			S_Spawn spawnPacket = new S_Spawn();
			foreach (Player go in _players.Values)
			{
				spawnPacket.Objects.Add(go.Info); 
			}
			
			Broadcast(spawnPacket);
		}

		public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

			if (type == GameObjectType.Player)
			{
				Player player = null;
				if (_players.Remove(objectId, out player) == false)
					return;

				player.OnLeaveGame();
				player.Room = null;

				// 본인한테 정보 전송
				S_LeaveGame leavePacket = new S_LeaveGame();
				player.Session.Send(leavePacket);
			}

			// 타인한테 정보 전송			
			S_Despawn despawnPacket = new S_Despawn();
			despawnPacket.ObjectIds.Add(objectId);
			Broadcast(despawnPacket);
			

			// 플레이어가 없으면서 룸이 로비로 사용하지 않는 경우 room을 삭제
			if(_players.Count == 0 && RoomId != 1)
			{
				GameLogic.Instance.Push(() => GameLogic.Instance.Remove(RoomId));
			}
		}

		public void RequestDuel(int playerId, int opponentId)
		{
			Int32 isOK = _players[playerId].Session.ServerState == PlayerServerState.ServerStateLobby ? 1 : 0;
            S_RequestSendOk requestSendOKpacket = new S_RequestSendOk() { SendOK = isOK };
			_players[playerId].Session.Send(requestSendOKpacket);

			if(isOK == 1)
			{	
				S_RequestDuel requestDuelpacket = new S_RequestDuel() { FromId = playerId };
				_players[opponentId].Session.Send(requestDuelpacket);
			}
        }

		public void RespondDuel(C_RespondDuel packet)
		{
			S_RespondDuel respondDuelpacket = new S_RespondDuel();
			respondDuelpacket.DuelOK = packet.DuelOK;

            Player Respond = _players[packet.FromId];
            Player opponent = _players[packet.ToId];

            // 대결을 신청한 상대에게 응답패킷을 보냄
			// 대결을 승낙하면 씬 전환을 해야하므로 본인에게도 다시 보냄
			if (respondDuelpacket.DuelOK == 1)
            {
				GameLogic.Instance.Push( () => 
				{
                    GameRoom room = GameLogic.Instance.Add();

                    respondDuelpacket.EnemyId = packet.FromId;
                    opponent.Session.HandleRespondDuel(respondDuelpacket, room.RoomId);

                    respondDuelpacket.EnemyId = packet.ToId;
                    Respond.Session.HandleRespondDuel(respondDuelpacket, room.RoomId);
                });				
            }
			else
			{
                respondDuelpacket.EnemyId = packet.FromId;
                opponent.Session.HandleRespondDuel(respondDuelpacket, 0);
            }
        }

		public void SelectMst(int playerId)
		{
			_playerReady[playerId] = true;

			int cnt = 0;
			foreach (bool ready in _playerReady.Values)
			{
				cnt = ready ? cnt + 1 : cnt;
			}

			if(cnt == _playerReady.Count)
			{
				int[] list = _players.Keys.ToArray();

                S_StartBattle packet = new S_StartBattle();
                Random random = new Random();
                packet.ArenaType = random.Next(0, (int)Arenas.End);
				int turnorder = ObjectManager.Instance.GetTurn(RoomId);
                for (int i = 0; i < (int)TargetType.End; ++i)
                {
                    int myid = list[i];
					int enemyid = list[(int)TargetType.Enemy - i];

                    packet.FromPokemon.Clear();
                    packet.ToPokemon.Clear();

                    packet.MyInfo = _players[myid].Info;
                    packet.EnemyInfo = _players[enemyid].Info;

                    for (int j = 0; j < 3; ++j)
					{
						packet.FromPokemon.Add(_players[myid].Pokemon[j].Id);
                        packet.ToPokemon.Add(_players[enemyid].Pokemon[j].Id);
						_players[myid].Pokemon[j].Info.State = new ConditionAbnormality();
                        _players[enemyid].Pokemon[j].Info.State = new ConditionAbnormality();
                    }
                    
                    packet.IsMyTurn = myid == turnorder;
					//_players[myid].Session.HandleCreatePlayer();
                    _players[myid].Session.Send(packet);
                }
            }
		}

		public void SetPlayerBySession(ClientSession session, LobbyPlayerInfo info)
		{
			Player player = ObjectManager.Instance.Add<Player>(RoomId);
			player.PlayerDbId = info.PlayerDbId;
			player.Info.Name = info.Name;
			player.Session = session;

			session.MyPlayer = player;

			EnterGame(player);
		}

		Player FindPlayer(Func<GameObject, bool> condition)
		{
			foreach (Player player in _players.Values)
			{
				if (condition.Invoke(player))
					return player;
			}

			return null;
		}

		int FindEnemyIdById(int playerId)
		{
            foreach (int id in _players.Keys)
            {
                if (id != playerId)
                    return id;
            }
			return -1;
        }
				
		public void Broadcast(IMessage packet)
		{
			foreach (Player p in _players.Values)
			{
				p.Session.Send(packet);
			}
		}
	}
}
