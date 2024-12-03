using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
	{        public void Turn(C_Turn packet)
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
            PokemonData pokemonData = player.Pokemon[0];
            PokemonData standardInfo = DataManager.PokemonDict[player.Pokemon[0].Id];
            if (pokemonData.Info.State.Fire > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                pokemonData.Info.Hp = pokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                pokemonData.Info.State.Fire--;
                result.FromData= pokemonData;
                resultlist.Add(result);
            }

            if (pokemonData.Info.State.Dot > 0)
            {
                BattleInfo result = new BattleInfo();
                result.StateInfo = new StateInfo();
                result.ApplyType = ApplyType.Dot;
                result.TargetType = TargetType.Oneself;
                result.StateInfo.SkillId = packet.TurnInfo.SkillNum;
                pokemonData.Info.Hp = pokemonData.Info.Hp - standardInfo.Info.Hp / 7;
                pokemonData.Info.State.Dot--;
                result.FromData = pokemonData;
                resultlist.Add(result);
            }

            if (pokemonData.Info.State.Poison > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                pokemonData.Info.Hp = pokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                pokemonData.Info.State.Poison--;
                result.FromData = pokemonData;
                resultlist.Add(result);
            }

            if (pokemonData.Info.State.Confusion > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Confusion;
                result.TargetType = TargetType.Oneself;
                bool recovery = random.Next(0, 100) > 60 ? true : false;

                pokemonData.Info.State.Confusion--;
                if (recovery)
                    pokemonData.Info.State.Confusion = 0;

                resultlist.Add(result);
            }

            if (pokemonData.Info.State.Sturn > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Sturn;
                result.TargetType = TargetType.Oneself;
                bool recovery = random.Next(0, 100) > 60 ? true : false;

                result.StateInfo.RecoverSturn = recovery;
                pokemonData.Info.State.Sturn--;
                if (recovery)
                    pokemonData.Info.State.Sturn = 0;

                resultlist.Add(result);
            }

            return resultlist;
        }

        void Pass(C_Turn packet)
        {
            S_TurnPass passPacket = new S_TurnPass();

            passPacket.PlayerId = packet.PlayerId;

            List<BattleInfo> turnInfo = DefaultTurn(packet);
            Util.AddRepeatedFieldToList(passPacket.TurnInfo, turnInfo);

            Broadcast(passPacket);
        }

        void Fight(C_Turn packet)
        {
            S_TurnBattle[] BattlePacket = new S_TurnBattle[(int)TargetType.End];

            BattlePacket[(int)TargetType.Oneself] = new S_TurnBattle();
            BattlePacket[(int)TargetType.Enemy] = new S_TurnBattle();

            int enemyId = FindEnemyIdById(packet.PlayerId);
           
            Player player = ObjectManager.Instance.Find(RoomId, packet.PlayerId);
            Player Enemy = ObjectManager.Instance.Find(RoomId, enemyId);
            PokemonData myData = player.Pokemon[0];
            PokemonData enemyData = Enemy.Pokemon[0];
            SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillNum];

            List<BattleInfo>[] result = new List<BattleInfo>[(int)TargetType.End];
            for (int index = 0; index < (int)TargetType.End; ++index)
                result[index] = new List<BattleInfo>();

            for (int index = 0; index < skillData.info.SkillEffect.Count; index++)
            {
                BattleInfo[] infos = Util.CalcBattle(myData, enemyData, skillData.info.SkillEffect[index]);
                for (int j = 0; j < (int)TargetType.End; ++j)
                    result[j].Add(infos[j]);
            }

            for (int  index = 0; index < (int)TargetType.End; ++index)
            {
                BattlePacket[index].PlayerId = packet.PlayerId;
                BattlePacket[index].SkillId = packet.TurnInfo.SkillNum;
                Util.AddRepeatedFieldToList(BattlePacket[index].Info, result[index]);
                Util.AddRepeatedFieldToList(BattlePacket[index].TurnInfo, DefaultTurn(packet));
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
            _playerReady[playerId] = true;

            int cnt = 0;
            foreach (bool ready in _playerReady.Values)
                cnt = ready ? cnt + 1 : cnt;

            if (cnt == _playerReady.Count)
            {
                int[] list = _players.Keys.ToArray();

                S_Turn packet = new S_Turn();
                int turnorder = ObjectManager.Instance.GetTurn(RoomId);
                for (int i = 0; i < (int)TargetType.End; ++i)
                {
                    int myid = list[i];
                    int enemyid = list[(int)TargetType.Enemy - i];

                    packet.MyTurn = myid == turnorder;

                    _players[myid].Session.Send(packet);
                }
            }
        }
    }
}
