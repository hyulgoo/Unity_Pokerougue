using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        private const int _maxPokemonCount = 2;
        public int RoomId { get; set; }
        public int CurrentTurnPlayerId { get; set; } = 0;
        private bool isWaitingPlayerTurnEnd = false;
        private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();
        private readonly Dictionary<int, bool> _playerReady = new Dictionary<int, bool>();

        // 전투가 시작돼 턴 패킷을 받을 수 있는 상태인가. 로비에서는 항상 false다.
        private bool _battleStarted;

        // 대전 신청 대기 목록(신청자 ID → 대상 ID). 응답 패킷을 이 기록과 대조한다.
        private readonly Dictionary<int, int> _duelRequests = new Dictionary<int, int>();

        public void Init()
        {
        }

        public void Update()
        {
            Flush();
        }

        private void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = PlayerManager.GetObjectTypeById(gameObject.Id);

            if (type == GameObjectType.Player)
            {
                if (gameObject is Player player)
                {
                    _players.Add(gameObject.Id, player);
                    _playerReady.Add(gameObject.Id, false);
                    player.Room = this;

                    // 본인한테 정보 전송
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);
                }
            }

            // 타인한테 정보 전송
            S_Spawn spawnPacket = new S_Spawn();
            foreach (Player go in _players.Values)
                spawnPacket.Objects.Add(go.Info);

            Broadcast(spawnPacket);
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = PlayerManager.GetObjectTypeById(objectId);

            if (type == GameObjectType.Player)
            {
                if (!_players.Remove(objectId, out Player player))
                    return;

                if (!_playerReady.Remove(objectId, out bool _))
                    return;

                _duelRequests.Remove(objectId);

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
            if (_players.Count == 0 && RoomId != 1)
            {
                GameLogic.Instance.Push(() => GameLogic.Instance.Remove(RoomId));
            }
            else if (_players.Count == 0 && RoomId == 1)
            {
                _players.Clear();
                _playerReady.Clear();
                isWaitingPlayerTurnEnd = false;
                CurrentTurnPlayerId = 0;
            }
        }

        private bool IsInLobby(Player player)
        {
            return player.Room == this && player.Session.ServerState == PlayerServerState.ServerStateLobby;
        }

        // playerId는 세션에서 얻은 신청자다.
        public void RequestDuel(int playerId, int enemytId)
        {
            if (!_players.TryGetValue(playerId, out Player player))
                return;

            // 신청자와 대상이 모두 이 방에 있고 로비 상태여야 한다.
            Player enemy = null;
            bool isOK = playerId != enemytId
                        && IsInLobby(player)
                        && _players.TryGetValue(enemytId, out enemy)
                        && IsInLobby(enemy);

            S_RequestSendOk requestSendOKpacket = new S_RequestSendOk { SendOK = isOK ? 1 : 0 };
            player.Session.Send(requestSendOKpacket);

            if (!isOK)
                return;

            _duelRequests[playerId] = enemytId;

            S_RequestDuel requestDuelpacket = new S_RequestDuel { FromId = playerId };
            enemy.Session.Send(requestDuelpacket);
        }

        // playerId는 세션에서 얻은 응답자다. 패킷의 FromId는 쓰지 않는다.
        public void RespondDuel(int playerId, C_RespondDuel packet)
        {
            // 패킷의 ToId(신청자)가 실제로 이 플레이어에게 신청한 기록이 있어야 한다.
            int requesterId = packet.ToId;
            if (!_duelRequests.TryGetValue(requesterId, out int targetId) || targetId != playerId)
                return;

            _duelRequests.Remove(requesterId);

            if (!_players.TryGetValue(playerId, out Player fromPlayer) ||
                !_players.TryGetValue(requesterId, out Player toPlayer))
                return;

            S_RespondDuel respondDuelpacket = new S_RespondDuel
            {
                DuelOK = packet.DuelOK
            };

            // 대결을 신청한 상대에게 응답패킷을 보냄
            // 대결을 승낙하면 씬 전환을 해야하므로 본인에게도 다시 보냄
            if (respondDuelpacket.DuelOK == 1)
            {
                GameLogic.Instance.Push(() =>
                {
                    // 이 잡이 실행되기 전에 한쪽이 방을 떠났거나 다른 대전에 들어갔으면 시작하지 않는다.
                    if (!IsInLobby(fromPlayer) || !IsInLobby(toPlayer))
                        return;

                    Room.GameRoom room = GameLogic.Instance.Add();

                    respondDuelpacket.EnemyId = playerId;
                    toPlayer.Session.HandleRespondDuel(respondDuelpacket, room.RoomId);

                    respondDuelpacket.EnemyId = requesterId;
                    fromPlayer.Session.HandleRespondDuel(respondDuelpacket, room.RoomId);
                });
            }
            else
            {
                respondDuelpacket.EnemyId = playerId;
                toPlayer.Session.HandleRespondDuel(respondDuelpacket, 0);
            }
        }

        // playerId는 세션에서 얻은 플레이어다. 패킷의 PlayerId는 쓰지 않는다.
        public void SelectPokemon(int playerId, C_SelectPokemon selectPacket)
        {
            // 두 명이 들어온 전투방에서, 전투 시작 전에, 플레이어당 한 번만 받는다.
            if (_battleStarted || _players.Count != (int)TargetType.End)
                return;

            if (!_players.TryGetValue(playerId, out Player player) ||
                !_playerReady.TryGetValue(playerId, out bool selected) || selected)
                return;

            if (player.Session.ServerState != PlayerServerState.ServerStateGame)
                return;

            // 수, 중복, 데이터에 있는 포켓몬인지 확인한다.
            if (selectPacket.PokemonList.Count != _maxPokemonCount ||
                selectPacket.PokemonList.Distinct().Count() != _maxPokemonCount)
                return;

            List<PokemonData> selectedPokemon = new List<PokemonData>();
            foreach (int pokemonId in selectPacket.PokemonList)
            {
                if (!DataManager.PokemonDict.TryGetValue(pokemonId, out PokemonData pokemonData))
                    return;

                selectedPokemon.Add(pokemonData);
            }

            player.Pokemon = selectedPokemon;
            _playerReady[playerId] = true;

            int cnt = 0;
            foreach (bool ready in _playerReady.Values)
                cnt = ready ? cnt + 1 : cnt;

            if (cnt != _players.Count)
                return;

            foreach (KeyValuePair<int, bool> value in _playerReady.ToList())
                _playerReady[value.Key] = false;

            int[] list = _players.Keys.ToArray();

            S_StartBattle packet = new S_StartBattle();
            Random random = new Random();
            packet.ArenaType = random.Next(0, (int)Arenas.End);
            CurrentTurnPlayerId = PlayerManager.Instance.GetCurrentTurnPlayerId(RoomId);
            for (int targetIndex = 0; targetIndex < (int)TargetType.End; ++targetIndex)
            {
                int myid = list[targetIndex];
                int enemyid = list[(int)TargetType.Enemy - targetIndex];

                packet.FromPokemon.Clear();
                packet.ToPokemon.Clear();

                packet.MyInfo = _players[myid].Info;
                packet.EnemyInfo = _players[enemyid].Info;

                for (int pokemonIndex = 0; pokemonIndex < _maxPokemonCount; ++pokemonIndex)
                {
                    packet.FromPokemon.Add(_players[myid].Pokemon[pokemonIndex].Id);
                    packet.ToPokemon.Add(_players[enemyid].Pokemon[pokemonIndex].Id);
                    _players[myid].Pokemon[pokemonIndex].Info.State = new ConditionAbnormality();
                    _players[enemyid].Pokemon[pokemonIndex].Info.State = new ConditionAbnormality();
                }

                packet.IsMyTurn = myid == CurrentTurnPlayerId;
                _players[myid].Session.Send(packet);
            }

            _battleStarted = true;
        }

        public void SetPlayerBySession(ClientSession session, LobbyPlayerInfo info)
        {
            // 이미 이 방에 들어와 있는 세션이 입장을 다시 요청하면 플레이어를 또 만들지 않는다.
            if (session.MyPlayer != null && session.MyPlayer.Room == this)
                return;

            Player player = PlayerManager.Instance.Add<Player>(RoomId);
            player.PlayerDbId = info.PlayerDbId;
            player.Info.Name = info.Name;
            player.Session = session;

            session.MyPlayer = player;

            EnterGame(player);
        }

        private Player FindPlayer(Func<GameObject, bool> condition)
        {
            return _players.Values.FirstOrDefault(condition.Invoke);
        }

        private int FindEnemyIdByMyId(int playerId)
        {
            foreach (int id in _players.Keys.Where(id => id != playerId))
                return id;
            return -1;
        }

        private void Broadcast(IMessage packet)
        {
            foreach (Player player in _players.Values)
                player.Session.Send(packet);
        }
    }
}