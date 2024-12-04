using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
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
                result.StateInfo = new StateInfo();
                result.ApplyType = ApplyType.Dot;
                result.TargetType = TargetType.Oneself;
                result.StateInfo.SkillId = packet.TurnInfo.SkillNum;
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

                result.StateInfo.RecoverSturn = recovery;
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

            PokemonData myData = player.Pokemon[0];
            PokemonData enemyData = Enemy.Pokemon[0];
            SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillNum];

            RepeatedField<BattleInfo> battleResult = new RepeatedField<BattleInfo>();

            foreach (SkillEffect skillEffect in skillData.info.SkillEffect)
            {
                RepeatedField<BattleInfo> battleInfo = Util.CalcBattle(myData, enemyData, skillEffect).Clone();
                battleResult.AddRange(battleInfo);
            }

            for (int index = 0; index < (int)TargetType.End; ++index)
            {
                BattlePacket[index].PlayerId = packet.PlayerId;
                BattlePacket[index].SkillId = packet.TurnInfo.SkillNum;
                BattlePacket[index].Info.AddRange(battleResult.Clone());
                BattlePacket[index].TurnInfo.AddRange(DefaultTurn(packet.Clone()));
            }

            _players[packet.PlayerId].Session.Send(BattlePacket[(int)TargetType.Oneself]);
            _players[enemyId].Session.Send(BattlePacket[(int)TargetType.Enemy]);
        }

        void PokeBall(C_Turn packet)
        {
            S_TurnPokeball pokeballPacket = new S_TurnPokeball();
            PokeballInfo info = new PokeballInfo();
            pokeballPacket.Info = info;

            Broadcast(pokeballPacket);
        }

        void Change(C_Turn packet)
        {
            S_TurnPokeball pokeballPacket = new S_TurnPokeball();
            PokeballInfo info = new PokeballInfo();
            pokeballPacket.Info = info;

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

            int cnt = 0;
            foreach (bool ready in _playerReady.Values)
                cnt = ready ? cnt + 1 : cnt;

            if (cnt != _playerReady.Count)
                return;
            
            int[] list = _players.Keys.ToArray();

            S_Turn packet = new S_Turn();
            int turnorder = PlayerManager.Instance.GetTurn(RoomId);
            for (int i = 0; i < (int)TargetType.End; ++i)
            {
                int myid = list[i];
                int enemyid = list[i + 1 == (int)TargetType.End ? 0 : i + 1];

                packet.MyTurn = myid == turnorder;

                _players[myid].Session.Send(packet);
            }            
        }
    }
}
