using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
    {
        const int confusionSkillId = 999;

        public void Turn(C_Turn packet)
        {
            isWaitingPlayerTurnEnd = true;

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

        bool DefaultTurn(C_Turn packet, ref RepeatedField<BattleInfo> battleInfoList)
        {
            Random random = new Random();
            Player player = PlayerManager.Instance.Find(RoomId, packet.PlayerId);
            Player enemy = PlayerManager.Instance.Find(RoomId, FindEnemyIdByMyId(packet.PlayerId));
            PokemonData myPokemonData = player.Pokemon[0];
            PokemonData enemyPokemonData = enemy.Pokemon[0];
            PokemonData standardInfo = DataManager.PokemonDict[player.Pokemon[0].Id];
            bool isSturnOrConfusionAttackOneself = false;


            if (myPokemonData.Info.State.Fire > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                myPokemonData.Info.State.Fire--;
                result.FromData= myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();
                result.StateFlag |= BattleStateFlag.DebuffFire;

                battleInfoList.Add(result);
            }

            if (myPokemonData.Info.State.Dot > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Dot;
                result.TargetType = TargetType.Oneself;
                result.LatingSkillId = packet.TurnInfo.SkillId;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 7;
                myPokemonData.Info.State.Dot--;
                result.FromData = enemyPokemonData.Clone();
                result.ToData = myPokemonData.Clone();
                result.StateFlag |= BattleStateFlag.DebuffDot;

                battleInfoList.Add(result);
            }

            if (myPokemonData.Info.State.Poison > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                myPokemonData.Info.State.Poison--;
                result.FromData = myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();
                result.StateFlag |= BattleStateFlag.DebuffPoison;

                battleInfoList.Add(result);
            }

            if (myPokemonData.Info.State.Confusion > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Confusion;
                result.TargetType = TargetType.Oneself;
                result.FromData = myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();

                bool recovery = random.Next(0, 100) > 60 ? true : false;
                if (recovery == false)
                {
                    myPokemonData.Info.State.Confusion -= 1;
                    result.StateFlag |= BattleStateFlag.DebuffConfusion;
                    if (myPokemonData.Info.State.Confusion == 0)
                        result.StateFlag |= BattleStateFlag.RecoveryConfusion;

                    battleInfoList.Add(result);

                    if (random.Next(0, 100) < 40)
                    {
                        result.LatingSkillId = confusionSkillId;
                        SkillData skillData = DataManager.SkillDict[confusionSkillId];
                        foreach (SkillEffect skillEffect in skillData.info.SkillEffect)
                        {
                            BattleInfo confusionInfo = Util.CalcBattle(myPokemonData, myPokemonData, skillEffect);
                            battleInfoList.Add(confusionInfo);
                        }

                        isSturnOrConfusionAttackOneself = true;
                    }
                }
                else
                {
                    result.StateFlag |= BattleStateFlag.RecoveryConfusion;
                    myPokemonData.Info.State.Confusion = 0;
                    battleInfoList.Add(result);
                }
            }

            if (myPokemonData.Info.State.Sturn > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Sturn;
                result.TargetType = TargetType.Oneself;
                result.FromData = myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();

                bool recovery = random.Next(0, 100) > 66 ? true : false;
                myPokemonData.Info.State.Sturn = recovery ? 0 : myPokemonData.Info.State.Sturn - 1;

                if (recovery == false)
                {
                    myPokemonData.Info.State.Sturn -= 1;
                    result.StateFlag |= BattleStateFlag.DebuffSturn;
                    if(myPokemonData.Info.State.Sturn == 0)
                        result.StateFlag |= BattleStateFlag.RecoverySturn;
                    isSturnOrConfusionAttackOneself = true;
                }
                else
                {
                    result.StateFlag |= BattleStateFlag.RecoverySturn;
                    myPokemonData.Info.State.Sturn = 0;
                }

                battleInfoList.Add(result);
            }

            return isSturnOrConfusionAttackOneself;
        }

        void Pass(C_Turn packet)
        {
            S_TurnPass passPacket = new S_TurnPass();

            passPacket.PlayerId = packet.PlayerId;
            RepeatedField<BattleInfo> battleInfoList = new RepeatedField<BattleInfo>();
            DefaultTurn(packet.Clone(), ref battleInfoList);
            passPacket.TurnInfo.AddRange(battleInfoList);

            Broadcast(passPacket);
        }

        void Fight(C_Turn packet)
        {
            S_TurnBattle battlePacket = new S_TurnBattle();

            int enemyId = FindEnemyIdByMyId(packet.PlayerId);
            Player player = PlayerManager.Instance.Find(RoomId, packet.PlayerId);
            Player enemy = PlayerManager.Instance.Find(RoomId, enemyId);

            PokemonData myPokemonData = player.Pokemon[0];
            PokemonData enemyPokemonData = enemy.Pokemon[0];

            RepeatedField<BattleInfo> battleTurnResult = new RepeatedField<BattleInfo>();
            RepeatedField<BattleInfo> battleFightResult = new RepeatedField<BattleInfo>();

            bool isSturn = DefaultTurn(packet, ref battleTurnResult);
            if (isSturn == false)
            {
                SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillId];
                foreach (SkillEffect skillEffect in skillData.info.SkillEffect)
                {
                    BattleInfo battleInfo = Util.CalcBattle(myPokemonData, enemyPokemonData, skillEffect);
                    battleFightResult.Add(battleInfo);
                }
            }

            battlePacket.PlayerId = packet.PlayerId;
            battlePacket.SkillId = packet.TurnInfo.SkillId;
            battlePacket.TurnInfo.AddRange(battleTurnResult);
            battlePacket.Info.AddRange(battleFightResult);

            Broadcast(battlePacket);

            Dictionary<int, bool> playerDualResultArray = new Dictionary<int, bool>();
            foreach (Player playerIter in _players.Values)
                playerDualResultArray.Add(playerIter.Id, playerIter.IsRemainPokemonExist());

            bool isDualEnd = false;
            foreach (bool isRemainPokemonExist in playerDualResultArray.Values)
            {
                if (isRemainPokemonExist == false)
                    isDualEnd = true;
            }

            if (isDualEnd == false)
                return;
            
            S_DualEnd dualEndPacket = new S_DualEnd();
            foreach (var playerDualResult in playerDualResultArray)
            {
                DualResultInfo dualResultInfo = new DualResultInfo();
                dualResultInfo.PlayerId = playerDualResult.Key;
                dualResultInfo.IsWin = playerDualResult.Value;

                dualEndPacket.DualResultInfo.Add(dualResultInfo);
            }

            Broadcast(dualEndPacket);            
        }

        void PokeBall(C_Turn packet)
        {
            S_TurnPokeball pokeballPacket = new S_TurnPokeball();
            pokeballPacket.BallType = packet.TurnInfo.BallType;
            Broadcast(pokeballPacket);
        }

        void Change(C_Turn packet)
        {
            S_TurnChange pokeballPacket = new S_TurnChange();            
            Broadcast(pokeballPacket);
        }

        void Runaway(C_Turn packet)
        {
            S_TurnRunaway runawayPacket = new S_TurnRunaway();
            Broadcast(runawayPacket);
        }

        public void TurnEnd(int playerId)
        {
            if (isWaitingPlayerTurnEnd == false)
                return;

            _playerReady[playerId] = true;

            int readyPlayerCount = 0;
            foreach (bool ready in _playerReady.Values)
                readyPlayerCount = ready ? ++readyPlayerCount : readyPlayerCount;

            if (readyPlayerCount != _playerReady.Count)
                return;

            CurrentTurnPlayerId = PlayerManager.Instance.GetCurrentTurnPlayerId(RoomId, false);
                        
            List<int> playerKeyArray = _players.Keys.ToList();
            for (int index = 0; index < playerKeyArray.Count; ++index)
            {
                int myId = playerKeyArray[index];
                int enemyid = playerKeyArray[index + 1 == playerKeyArray.Count ? 0 : index + 1];

                S_Turn turnPacket = new S_Turn();
                turnPacket.MyTurn = myId == CurrentTurnPlayerId;

                _playerReady[myId] = false;
                _players[myId].Session.Send(turnPacket);
            }
            
            isWaitingPlayerTurnEnd = false;
        }

        public void ChangePokemon(C_ChangeFalldownPokemon packet)
        {
            S_ChangePokemon changePokemonPacket = new S_ChangePokemon();
            changePokemonPacket.PlayerId = packet.PlayerId;
            changePokemonPacket.ChangePokemonId = packet.ChangePokemonId;
            
            List<PokemonData> pokemonDataList = _players[packet.PlayerId].Pokemon;
            for (int index = 0; index < pokemonDataList.Count; ++index)
            {
                if (pokemonDataList[index].Id != packet.ChangePokemonId)
                    continue;
            
                (pokemonDataList[0], pokemonDataList[index]) = (pokemonDataList[index], pokemonDataList[0]);
                Broadcast(changePokemonPacket);
                return;
            }
            
            Debug.Assert(false, "Cannot Found ChangePokemon");                      
        }
    }
}
