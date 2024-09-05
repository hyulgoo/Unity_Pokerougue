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
            PokemonInfo pokemonInfo = player.Pokemon[0].info;
            PokemonInfo standardInfo = DataManager.MonsterDict[player.Pokemon[0].id].info;

            if (pokemonInfo.State.Fire != 0)
            {
                BattleInfo result = new BattleInfo();
                result.SkillType = SkillType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                pokemonInfo.Hp = pokemonInfo.Hp - standardInfo.Hp / 20;
                pokemonInfo.State.Fire--;
                result.MyInfo = pokemonInfo;
                resultlist.Add(result);
            }

            if (pokemonInfo.State.Dot != 0)
            {
                BattleInfo result = new BattleInfo();
                result.StateInfo = new StateInfo();
                result.SkillType = SkillType.Dot;
                result.TargetType = TargetType.Oneself;
                result.StateInfo.SkillId = packet.TurnInfo.SkillNum;
                pokemonInfo.Hp = pokemonInfo.Hp - standardInfo.Hp / 7;
                pokemonInfo.State.Dot--;
                result.MyInfo = pokemonInfo;
                resultlist.Add(result);
            }

            if (pokemonInfo.State.Poison != 0)
            {
                BattleInfo result = new BattleInfo();
                result.SkillType = SkillType.StatusEffect;
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
            // 전투 정보를 담는
            S_TurnBattle[] BattlePacket = new S_TurnBattle[(int)TargetType.End];

            BattlePacket[(int)TargetType.Oneself] = new S_TurnBattle();
            BattlePacket[(int)TargetType.Enemy] = new S_TurnBattle();

            int enemyId = FindEnemyIdById(packet.PlayerId);
           
            Player player = ObjectManager.Instance.Find(RoomId, packet.PlayerId);
            Player Enemy = ObjectManager.Instance.Find(RoomId, enemyId);
            PokemonData data = player.Pokemon[0];
            PokemonData enemyData = Enemy.Pokemon[0];
            SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillNum];

            List<BattleInfo>[] result = new List<BattleInfo>[(int)TargetType.End];
            for (int i = 0; i < (int)TargetType.End; ++i)
            {
                result[i] = new List<BattleInfo>();
            }

            for (int i = 0; i < skillData.info.SkillEffect.Count; i++)
            {
                BattleInfo[] infos = Util.CalcBattle(data, enemyData, skillData.info.SkillEffect[i]);
                for (int j = 0; j < (int)TargetType.End; ++j)
                {
                    result[j].Add(infos[j]);
                }
            }

            for (int i = 0; i < (int)TargetType.End; ++i)
            {
                BattlePacket[i].PlayerId = packet.PlayerId;
                Util.AddtoTargetList(BattlePacket[i].Info, result[i]);
                Util.AddtoTargetList(BattlePacket[i].TurnInfo, DefaultTurn(packet));
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
            {
                cnt = ready ? cnt + 1 : cnt;
            }

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
