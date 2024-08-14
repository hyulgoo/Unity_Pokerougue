using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
	{
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();

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
				player.Room = this;

				// 본인한테 정보 전송
				{
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);
				}
			}

			// 타인한테 정보 전송
			{
				S_Spawn spawnPacket = new S_Spawn();
				foreach(Player go in _players.Values)
					spawnPacket.Objects.Add(go.Info);
				
				Broadcast(spawnPacket);
			}
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
				{
					S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}
			}			
			else
			{
				return;
			}

			// 타인한테 정보 전송
			{
				S_Despawn despawnPacket = new S_Despawn();
				despawnPacket.ObjectIds.Add(objectId);
				Broadcast(despawnPacket);
			}

			if(_players.Count == 0)
			{
				GameLogic.Instance.Remove(RoomId);
			}
		}

		public void RequestDuel(int playerId, int opponentId)
		{
			Int32 isOK = _players[playerId].Session.ServerState == PlayerServerState.ServerStateLobby ? 1 : 0;
            S_RequestSendOk requestSendOKpacket = new S_RequestSendOk() { SendOK = isOK };
			_players[playerId].Session.Send(requestSendOKpacket);

			if(isOK == 1)
			{	
				S_RequestDuel requestDuelpacket = new S_RequestDuel() { RequestId = playerId };
				_players[opponentId].Session.Send(requestDuelpacket);
			}
        }

		public void RespondDuel(int playerId, int opponentId, bool duelOk)
		{
            Int32 isOK = duelOk ? 1 : 0;
            S_RespondDuel respondDuelpacket = new S_RespondDuel() { DuelOK = isOK };

            Player player = _players[playerId];
            Player opponentplayer = _players[opponentId];

            // 대결을 신청한 상대에게 응답패킷을 보냄

			// 대결을 승낙하면 씬 전환을 해야하므로 본인에게도 다시 보냄
			if(duelOk)
            {
				GameLogic.Instance.Push( () => 
					{
                        GameRoom room = GameLogic.Instance.Add();
                        int roomId = room.RoomId;

                        respondDuelpacket.OpponentId = playerId;
                        opponentplayer.Session.HandleRespondDuel(respondDuelpacket, roomId);

                        respondDuelpacket.OpponentId = opponentId;
                        player.Session.HandleRespondDuel(respondDuelpacket, roomId);
                    }
					);				
            }
			else
			{
                respondDuelpacket.OpponentId = playerId;
                opponentplayer.Session.HandleRespondDuel(respondDuelpacket, 0);
            }
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
				
		public void Broadcast(IMessage packet)
		{
			foreach (Player p in _players.Values)
			{
				p.Session.Send(packet);
			}
		}
	}
}
