using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
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
				S_RequestDuel requestDuelpacket = new S_RequestDuel() { RequestId = playerId };
				_players[opponentId].Session.Send(requestDuelpacket);
			}
        }

		public void RespondDuel(C_RespondDuel packet)
		{
			S_RespondDuel respondDuelpacket = new S_RespondDuel();
			respondDuelpacket.DuelOK = packet.DuelOK;

            Player Respond = _players[packet.RespondId];
            Player opponent = _players[packet.EnemyId];

            // 대결을 신청한 상대에게 응답패킷을 보냄
			// 대결을 승낙하면 씬 전환을 해야하므로 본인에게도 다시 보냄
			if (respondDuelpacket.DuelOK == 1)
            {
				GameLogic.Instance.Push( () => 
				{
                    GameRoom room = GameLogic.Instance.Add();

                    respondDuelpacket.EnemyId = packet.RespondId;
                    opponent.Session.HandleRespondDuel(respondDuelpacket, room.RoomId);

                    respondDuelpacket.EnemyId = packet.EnemyId;
                    Respond.Session.HandleRespondDuel(respondDuelpacket, room.RoomId);
                });				
            }
			else
			{
                respondDuelpacket.EnemyId = packet.RespondId;
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
                for (int i = 0; i < 2; ++i)
                {
                    int myid = list[i];
                    int enemyid = i == 0 ? list[1] : list[0];

                    packet.MyMst.Clear();
                    packet.EnemyMst.Clear();

                    packet.MyInfo = _players[myid].Info;
                    packet.EnemyInfo = _players[enemyid].Info;

                    for (int j = 0; j < 3; ++j)
					{
						packet.MyMst.Add(_players[myid].Pokemon[j].id);
                        packet.EnemyMst.Add(_players[enemyid].Pokemon[j].id);
						_players[myid].Pokemon[j].info.State = new ConditionAbnormality();
                        _players[enemyid].Pokemon[j].info.State = new ConditionAbnormality();
                    }
                    
                    packet.MyTurn = myid == turnorder ? 1 : 0;

                    _players[myid].Session.Send(packet);
                }
            }
		}

		public void Turn(C_Turn packet)
        {			
			switch (packet.TurnInfo.Action)
			{
				case ActionType.Pass:
					Pass(packet);
					break;
				case ActionType.Fight:
					Fight(packet);					
                    break;
				case ActionType.Pokeball:
					PokeBall(packet);
                    break;
				case ActionType.Change:
					Change(packet);
                    break;
				case ActionType.Runaway:
					Runaway(packet);
					break;
			}
		}

        List<BattleInfo> DefaultTurn(C_Turn packet)
        {
            List<BattleInfo> resultlist = new List<BattleInfo>();
			Random random = new Random();
            Player player = ObjectManager.Instance.Find(RoomId, packet.PlayerId);
            PokemonInfo pokemonInfo = player.Pokemon[0].info;
            PokemonInfo standardInfo = DataManager.MonsterDict[player.Pokemon[0].id].info;

            if (pokemonInfo.State.Fire != 0)
            {
                BattleInfo result = new BattleInfo();
				result.SkillType = SkillType.Dot;
				result.TargetType = TargetType.Oneself;
                pokemonInfo.Hp = pokemonInfo.Hp - standardInfo.Hp / 20;
                pokemonInfo.State.Fire--;
                result.MyInfo = pokemonInfo;
                resultlist.Add(result);
            }

            if (pokemonInfo.State.Dot != 0)
            {
                BattleInfo result = new BattleInfo();
                result.SkillType = SkillType.Dot;
                result.TargetType = TargetType.Oneself;
                pokemonInfo.Hp = pokemonInfo.Hp - standardInfo.Hp / 20;
                pokemonInfo.State.Dot--;
                result.MyInfo = pokemonInfo;
                resultlist.Add(result);
            }

            if (pokemonInfo.State.Poison != 0)
            {
                BattleInfo result = new BattleInfo();
                result.SkillType = SkillType.Dot;
                result.TargetType = TargetType.Oneself;
                pokemonInfo.Hp = pokemonInfo.Hp - standardInfo.Hp / 20;
                pokemonInfo.State.Poison--;
                result.MyInfo = pokemonInfo;
                resultlist.Add(result);
            }

            if (pokemonInfo.State.Confusion != 0)
            {
                BattleInfo result = new BattleInfo();
				result.SkillType = SkillType.Confusion;
                result.TargetType = TargetType.Oneself;
                bool recovery = random.Next(0, 100) > 60 ? true : false;

                pokemonInfo.State.Confusion--;
                if (recovery)
                    pokemonInfo.State.Confusion = 0;

                resultlist.Add(result);
            }

            if (pokemonInfo.State.Sturn != 0)
            {
                BattleInfo result = new BattleInfo();
                result.SkillType = SkillType.Sturn;
                result.TargetType = TargetType.Oneself;
                bool recovery = random.Next(0, 100) > 60 ? true : false;

				result.StateInfo.RecoverFromSturn = recovery;
                pokemonInfo.State.Sturn--;
                if (recovery)
					pokemonInfo.State.Sturn = 0;

                resultlist.Add(result);
            }

            return resultlist;
        }

		void Pass(C_Turn packet)
        {
            S_TurnPass passPacket = new S_TurnPass();

            passPacket.PlayerId = packet.PlayerId;

            List<BattleInfo> turnInfo = DefaultTurn(packet);
            Util.AddtoTargetList(passPacket.TurnInfo, turnInfo);

			Broadcast(passPacket);
        }

        void Fight(C_Turn packet)
		{
            S_TurnBattle battlePacket = new S_TurnBattle();
            int enemyId = 0;

            foreach (int id in _players.Keys)
            {
                if (id != packet.PlayerId)
                    enemyId = id;
            }

            battlePacket.PlayerId = packet.PlayerId;

            Player player = ObjectManager.Instance.Find(RoomId, packet.PlayerId);
			Player Enemy = ObjectManager.Instance.Find(RoomId, enemyId);
			PokemonData data = player.Pokemon[0];
			PokemonData enemyData = Enemy.Pokemon[0];
			SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillNum];

            List<BattleInfo> result = new List<BattleInfo>();

			for (int i = 0; i < skillData.info.SkillEffect.Count; i++) 
			{
                result.Add(Util.CalcDamage(data, enemyData, skillData.info.SkillEffect[i]));
            }

            Util.AddtoTargetList(battlePacket.TurnInfo, result);
            Util.AddtoTargetList(battlePacket.Info, DefaultTurn(packet));

			Broadcast(battlePacket);
        }

		void PokeBall(C_Turn packet)
        {
            S_TurnPokeball pokeballPacket = new S_TurnPokeball();
            pokeballPacket.PlayerId = packet.PlayerId; 
			PokeballInfo info = new PokeballInfo();
			pokeballPacket.Info = info;

			Broadcast(pokeballPacket);
        }

		void Change(C_Turn packet)
        {
            S_TurnPokeball pokeballPacket = new S_TurnPokeball();
            pokeballPacket.PlayerId = packet.PlayerId;
            PokeballInfo info = new PokeballInfo();
            pokeballPacket.Info = info;

            Broadcast(pokeballPacket);
        }

		void Runaway(C_Turn packet)
		{
            S_TurnRunaway runawayPacket = new S_TurnRunaway();
            runawayPacket.PlayerId = packet.PlayerId;

            Broadcast(runawayPacket);
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
				
		public void Broadcast(IMessage packet)
		{
			foreach (Player p in _players.Values)
			{
				p.Session.Send(packet);
			}
		}
	}
}
