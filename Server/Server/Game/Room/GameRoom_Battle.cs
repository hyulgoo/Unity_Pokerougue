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

        RepeatedField<BattleInfo> DefaultTurn(C_Turn packet)
        {
            RepeatedField<BattleInfo> resultlist = new RepeatedField<BattleInfo>();
            Random random = new Random();
            Player player = PlayerManager.Instance.Find(RoomId, packet.PlayerId);
            PokemonData myPokemonData = player.Pokemon[0];
            PokemonData standardInfo = DataManager.PokemonDict[player.Pokemon[0].Id];

            if (myPokemonData.Info.State.Fire > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                myPokemonData.Info.State.Fire--;
                result.FromData= myPokemonData;
                resultlist.Add(result);
            }

            if (myPokemonData.Info.State.Dot > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Dot;
                result.TargetType = TargetType.Oneself;
                result.LatingSkillId = packet.TurnInfo.SkillId;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 7;
                myPokemonData.Info.State.Dot--;
                result.FromData = myPokemonData;
                resultlist.Add(result);
            }

            if (myPokemonData.Info.State.Poison > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                myPokemonData.Info.State.Poison--;
                result.FromData = myPokemonData;
                resultlist.Add(result);
            }

            if (myPokemonData.Info.State.Confusion > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Confusion;
                result.TargetType = TargetType.Oneself;
                bool recovery = random.Next(0, 100) > 60 ? true : false;

                myPokemonData.Info.State.Confusion--;
                if (recovery)
                    myPokemonData.Info.State.Confusion = 0;

                resultlist.Add(result);
            }

            if (myPokemonData.Info.State.Sturn > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Sturn;
                result.TargetType = TargetType.Oneself;

                bool recovery = random.Next(0, 100) > 60 ? true : false;
                myPokemonData.Info.State.Sturn--;
                if (recovery)
                    myPokemonData.Info.State.Sturn = 0;

                resultlist.Add(result);
            }

            return resultlist;
        }

        void Pass(C_Turn packet)
        {
            S_TurnPass passPacket = new S_TurnPass();

            passPacket.PlayerId = packet.PlayerId;
            passPacket.TurnInfo.AddRange(DefaultTurn(packet));

            Broadcast(passPacket);
        }

        void Fight(C_Turn packet)
        {
            S_TurnBattle[] BattlePacket = new S_TurnBattle[(int)TargetType.End];
            for(int index = 0; index < (int)TargetType.End; ++index)
                BattlePacket[index] = new S_TurnBattle();

            int enemyId = FindEnemyIdById(packet.PlayerId);
            Player player = PlayerManager.Instance.Find(RoomId, packet.PlayerId);
            Player Enemy = PlayerManager.Instance.Find(RoomId, enemyId);

            PokemonData myPokemonData = player.Pokemon[0];
            PokemonData enemyPokemonData = Enemy.Pokemon[0];
            SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillId];

            RepeatedField<BattleInfo> battleResult = new RepeatedField<BattleInfo>();

            foreach (SkillEffect skillEffect in skillData.info.SkillEffect)
            {
                RepeatedField<BattleInfo> battleInfo = Util.CalcBattle(myPokemonData, enemyPokemonData, skillEffect).Clone();
                battleResult.AddRange(battleInfo);
            }

            for (int index = 0; index < (int)TargetType.End; ++index)
            {
                BattlePacket[index].PlayerId = packet.PlayerId;
                BattlePacket[index].SkillId = packet.TurnInfo.SkillId;
                BattlePacket[index].Info.AddRange(battleResult.Clone());
                BattlePacket[index].TurnInfo.AddRange(DefaultTurn(packet.Clone()));
            }

            _players[packet.PlayerId].Session.Send(BattlePacket[(int)TargetType.Oneself]);
            _players[enemyId].Session.Send(BattlePacket[(int)TargetType.Enemy]);
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

            int readyCount = 0;
            foreach (bool ready in _playerReady.Values)
                readyCount = ready ? ++readyCount : readyCount;

            if (readyCount != _playerReady.Count)
                return;

            int[] playerKeyArray = _players.Keys.ToArray();
            CurrentTurnPlayerId = PlayerManager.Instance.GetCurrentTurnPlayerId(RoomId);
            for (int index = 0; index < (int)TargetType.End; ++index)
            {
                int myId = playerKeyArray[index];
                int enemyid = playerKeyArray[index + 1 == (int)TargetType.End ? 0 : index + 1];

                S_Turn packet = new S_Turn();
                packet.MyTurn = myId == CurrentTurnPlayerId;

                _playerReady[myId] = false;
                _players[myId].Session.Send(packet);
            }

            isWaitingPlayerTurnEnd = false;
        }

        public void ChangeFalldownPokemon(C_ChangeFalldownPokemon packet)
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
