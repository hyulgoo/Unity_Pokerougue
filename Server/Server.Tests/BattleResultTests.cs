using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Room;
using Xunit;

namespace Server.Tests
{
    // 전투 결과 판정, 라운드 순서, 기술 효과 (PK-R02)
    // StartBattle은 두 사람 모두 토대부기(388, 풀·땅)·초염몽(392, 불꽃·격투)을 고르고 토대부기가 먼저 나온다.
    public class BattleResultTests
    {
        private const int Torterra = 388;
        private const int Infernape = 392;
        private const int GigaDrain = 202;
        private const int Crunch = 242;
        private const int LeechSeed = 73;
        private const int FlareBlitz = 394;
        private const int CalmMind = 347;
        private const int Explosion = 153;

        // 명중률 100의 공격 기술. 명중 판정은 random.Next(0, 101) > Accuracy 라서 빗나가지 않는다.
        private static int SureHitAttack(ClientSession session)
        {
            return session.MyPlayer.Pokemon[0].Info.SkillId.First(id =>
            {
                SkillEffect effect = DataManager.SkillDict[id].info.SkillEffect[0];
                return (effect.ApplyType == ApplyType.Atk || effect.ApplyType == ApplyType.Spa)
                       && effect.Accuracy >= 100;
            });
        }

        private static List<S_DuelEnd> SentDuelEnds(ClientSession session)
        {
            return TestWorld.Sent(session, MsgId.SDuelEnd, S_DuelEnd.Parser);
        }

        // actorId가 행동한 결과 패킷
        private static S_TurnBattle FightPacket(ClientSession session, int actorId)
        {
            return TestWorld.Sent(session, MsgId.STurnBattle, S_TurnBattle.Parser).Last(packet => packet.PlayerId == actorId);
        }

        // 라운드가 끝날 때의 데미지·순풍 종료 패킷
        private static S_TurnBattle ResidualPacket(ClientSession session)
        {
            return TestWorld.Sent(session, MsgId.STurnBattle, S_TurnBattle.Parser).Last(packet => packet.PlayerId == 0);
        }

        private static List<int> ActorOrder(ClientSession session)
        {
            return TestWorld.Sent(session, MsgId.STurnBattle, S_TurnBattle.Parser)
                .Where(packet => packet.PlayerId != 0).Select(packet => packet.PlayerId).ToList();
        }

        private static Google.Protobuf.Collections.RepeatedField<SkillEffect> Effects(int skillId)
        {
            return DataManager.SkillDict[skillId].info.SkillEffect;
        }

        // 명중 90인 씨뿌리기를 테스트에서는 반드시 맞게 한다.
        private static int SureSeed()
        {
            Effects(LeechSeed)[0].Accuracy = 100;
            return LeechSeed;
        }

        // 깨물어부수기의 부가효과를 바꿔 원하는 효과를 정해진 확률로 일으킨다.
        private static int CrunchWith(ApplyType applyType, int chance = 100)
        {
            Effects(Crunch)[1] = new SkillEffect
            {
                ApplyType = applyType,
                Target = TargetType.Enemy,
                Type = Google.Protobuf.Protocol.Type.Dark,
                Value = 1,
                Accuracy = chance
            };
            return Crunch;
        }

        private static int MaxHp(PokemonData pokemon)
        {
            return DataManager.PokemonDict[pokemon.Id].Info.Hp;
        }

        private static PokemonData FreshPokemon(int index)
        {
            return DataManager.PokemonDict.Values.ElementAt(index).Clone();
        }

        private static SkillEffect Effect(ApplyType applyType, TargetType target, int value)
        {
            return new SkillEffect { ApplyType = applyType, Target = target, Type = Google.Protobuf.Protocol.Type.Normal, Value = value, Accuracy = 100 };
        }

        [Fact]
        public void 빗나간_공격도_공격한_포켓몬과_대상을_싣는다()
        {
            TestWorld.LoadData();
            PokemonData attacker = FreshPokemon(0);
            PokemonData target = FreshPokemon(1);
            SkillEffect alwaysMiss = Effect(ApplyType.Atk, TargetType.Enemy, 80);
            alwaysMiss.Accuracy = -1;

            BattleInfo info = Util.CalcBattle(attacker, target, alwaysMiss);

            Assert.True((info.StateFlag & BattleStateFlag.Miss) == BattleStateFlag.Miss);
            Assert.Equal(attacker.Id, info.FromData.Id);
            Assert.Equal(target.Id, info.ToData.Id);
        }

        [Fact]
        public void 반동은_준_피해의_value_퍼센트다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(owner, Infernape);
            Effects(FlareBlitz).First(e => e.ApplyType == ApplyType.Burn).Accuracy = 0;
            int recoilPercent = Effects(FlareBlitz).First(e => e.ApplyType == ApplyType.Atk && e.Target == TargetType.Oneself).Value;
            PokemonData attacker = owner.MyPlayer.Pokemon[0];
            int attackerHpBefore = attacker.Info.Hp;
            int targetHpBefore = other.MyPlayer.Pokemon[0].Info.Hp;

            TestWorld.Round(room, owner, TestWorld.Fight(FlareBlitz), other, TestWorld.Pass());

            int dealt = targetHpBefore - other.MyPlayer.Pokemon[0].Info.Hp;
            Assert.True(dealt > 0);
            Assert.Equal(Math.Max(1, dealt * recoilPercent / 100), attackerHpBefore - attacker.Info.Hp);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(100, true)]
        public void 부가효과의_accuracy는_발동_확률이고_발동하지_않아도_빗나감이_아니다(int chance, bool expectDrop)
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int crunch = CrunchWith(ApplyType.DebuffDef, chance);
            int defBefore = other.MyPlayer.Pokemon[0].Info.Def;

            TestWorld.Round(room, owner, TestWorld.Fight(crunch), other, TestWorld.Pass());

            List<BattleInfo> infos = FightPacket(owner, owner.MyPlayer.Id).Info.ToList();
            Assert.DoesNotContain(infos, info => (info.StateFlag & BattleStateFlag.Miss) == BattleStateFlag.Miss);
            Assert.Equal(expectDrop, infos.Any(info => info.ApplyType == ApplyType.DebuffDef));
            Assert.Equal(expectDrop, other.MyPlayer.Pokemon[0].Info.Def < defBefore);
        }

        [Fact]
        public void 풀_타입에게는_씨뿌리기가_듣지_않는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();

            TestWorld.Round(room, owner, TestWorld.Fight(SureSeed()), other, TestWorld.Pass());

            Assert.Equal(0, other.MyPlayer.Pokemon[0].Info.State.Dot);
            BattleInfo seed = FightPacket(owner, owner.MyPlayer.Id).Info.Single(info => info.ApplyType == ApplyType.Dot);
            Assert.True((seed.StateFlag & BattleStateFlag.Noneeffective) == BattleStateFlag.Noneeffective);
        }

        [Fact]
        public void 씨뿌리기는_라운드가_끝날_때_최대_HP의_8분의_1을_빼앗아_상대에게_준다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(other, Infernape);
            PokemonData seeded = other.MyPlayer.Pokemon[0];
            int seededHpBefore = seeded.Info.Hp;
            owner.MyPlayer.Pokemon[0].Info.Hp = 10;

            TestWorld.Round(room, owner, TestWorld.Fight(SureSeed()), other, TestWorld.Pass());

            Assert.Equal(seededHpBefore - MaxHp(seeded) / 8, seeded.Info.Hp);
            Assert.Equal(10 + MaxHp(seeded) / 8, owner.MyPlayer.Pokemon[0].Info.Hp);

            BattleInfo dot = ResidualPacket(other).TurnInfo
                .Single(info => (info.StateFlag & BattleStateFlag.DebuffDot) == BattleStateFlag.DebuffDot);
            Assert.Equal(LeechSeed, dot.LatingSkillId);
            Assert.Equal(other.MyPlayer.Id, dot.ToId);
        }

        [Fact]
        public void 씨뿌리기는_교체되면_풀린다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(other, Infernape);
            TestWorld.Round(room, owner, TestWorld.Fight(SureSeed()), other, TestWorld.Pass());
            PokemonData seeded = other.MyPlayer.Pokemon[0];
            Assert.True(seeded.Info.State.Dot > 0);
            TestWorld.EndTurn(room, owner, other);

            TestWorld.Round(room, owner, TestWorld.Pass(), other, TestWorld.Change(Torterra));

            Assert.Equal(0, seeded.Info.State.Dot);
        }

        [Fact]
        public void 흡수_기술은_준_피해의_절반을_회복한다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            owner.MyPlayer.Pokemon[0].Info.Hp = 10;
            int otherHpBefore = other.MyPlayer.Pokemon[0].Info.Hp;

            TestWorld.Round(room, owner, TestWorld.Fight(GigaDrain), other, TestWorld.Pass());

            int dealt = otherHpBefore - other.MyPlayer.Pokemon[0].Info.Hp;
            Assert.True(dealt > 0);
            Assert.Equal(10 + Math.Max(1, dealt / 2), owner.MyPlayer.Pokemon[0].Info.Hp);
        }

        [Fact]
        public void 상대의_마지막_포켓몬을_쓰러뜨리면_공격한_쪽이_이긴다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            // 나와 있는 포켓몬만 HP 1로 남기고 나머지는 이미 쓰러진 상태로 둔다.
            foreach (PokemonData pokemon in other.MyPlayer.Pokemon)
                pokemon.Info.Hp = 0;
            other.MyPlayer.Pokemon[0].Info.Hp = 1;

            TestWorld.Round(room, owner, TestWorld.Fight(SureHitAttack(owner)), other, TestWorld.Pass());

            Assert.True(SentDuelEnds(owner).Single().IsWin);
            Assert.False(SentDuelEnds(other).Single().IsWin);
        }

        [Fact]
        public void 도주한_쪽이_진다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();

            room.Turn(owner.MyPlayer.Id, TestWorld.Runaway());

            Assert.False(SentDuelEnds(owner).Single().IsWin);
            Assert.True(SentDuelEnds(other).Single().IsWin);
        }

        [Fact]
        public void 스피드가_빠른_쪽이_먼저_행동한다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(owner, Infernape);
            Assert.True(room.EffectiveSpeed(owner.MyPlayer.Id) > room.EffectiveSpeed(other.MyPlayer.Id));

            // 느린 쪽이 먼저 골라도 빠른 쪽이 먼저 움직인다.
            TestWorld.Round(room, other, TestWorld.Fight(SureSeed()), owner, TestWorld.Fight(CalmMind));

            Assert.Equal(new[] { owner.MyPlayer.Id, other.MyPlayer.Id }, ActorOrder(owner));
        }

        [Fact]
        public void 우선도가_높은_기술은_느려도_먼저_나간다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(owner, Infernape);
            int seed = SureSeed();
            DataManager.SkillDict[seed].info.Priority = 1;

            TestWorld.Round(room, owner, TestWorld.Fight(CalmMind), other, TestWorld.Fight(seed));

            Assert.Equal(new[] { other.MyPlayer.Id, owner.MyPlayer.Id }, ActorOrder(owner));
        }

        [Fact]
        public void 교체는_기술보다_먼저_일어나고_공격은_들어온_포켓몬이_맞는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(owner, Infernape);
            PokemonData outgoing = other.MyPlayer.Pokemon[0];

            TestWorld.Round(room, owner, TestWorld.Fight(FlareBlitz), other, TestWorld.Change(Infernape));

            List<MsgId> sent = TestWorld.SentIds(other);
            Assert.True(sent.IndexOf(MsgId.SChangePokemon) < sent.IndexOf(MsgId.STurnBattle));
            PokemonData incoming = other.MyPlayer.Pokemon[0];
            Assert.Equal(Infernape, incoming.Id);
            Assert.True(incoming.Info.Hp < MaxHp(incoming));
            Assert.Equal(MaxHp(outgoing), outgoing.Info.Hp);
        }

        [Fact]
        public void 순풍은_4턴_동안_스피드를_2배로_한다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(owner, Infernape);
            Effects(CalmMind).Clear();
            Effects(CalmMind).Add(Effect(ApplyType.Tailwind, TargetType.Oneself, 4));
            int speed = owner.MyPlayer.Pokemon[0].Info.Spe;

            TestWorld.Round(room, owner, TestWorld.Fight(CalmMind), other, TestWorld.Pass());
            Assert.Equal(speed * 2, room.EffectiveSpeed(owner.MyPlayer.Id));

            for (int round = 2; round <= 3; ++round)
            {
                TestWorld.EndTurn(room, owner, other);
                TestWorld.Round(room, owner, TestWorld.Pass(), other, TestWorld.Pass());
                Assert.Equal(speed * 2, room.EffectiveSpeed(owner.MyPlayer.Id));
            }

            TestWorld.EndTurn(room, owner, other);
            TestWorld.Round(room, owner, TestWorld.Pass(), other, TestWorld.Pass());
            Assert.Equal(speed, room.EffectiveSpeed(owner.MyPlayer.Id));
            Assert.Contains(ResidualPacket(owner).TurnInfo,
                info => (info.StateFlag & BattleStateFlag.TailwindEnd) == BattleStateFlag.TailwindEnd);
        }

        [Fact]
        public void 마비되면_스피드가_절반이다()
        {
            (GameRoom room, ClientSession owner, _) = TestWorld.StartBattle();
            int speed = owner.MyPlayer.Pokemon[0].Info.Spe;

            owner.MyPlayer.Pokemon[0].Info.State.Paralysis = 1;

            Assert.Equal(speed / 2, room.EffectiveSpeed(owner.MyPlayer.Id));
        }

        [Theory]
        [InlineData(ApplyType.Burn)]
        [InlineData(ApplyType.Paralysis)]
        public void 화상과_마비는_부가효과로_걸리고_한_가지만_걸린다(ApplyType status)
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            ConditionAbnormality state = other.MyPlayer.Pokemon[0].Info.State;

            TestWorld.Round(room, owner, TestWorld.Fight(CrunchWith(status)), other, TestWorld.Pass());

            Assert.Equal(status == ApplyType.Burn ? 1 : 0, state.Fire);
            Assert.Equal(status == ApplyType.Paralysis ? 1 : 0, state.Paralysis);
            Assert.Contains(FightPacket(owner, owner.MyPlayer.Id).Info, info => info.ApplyType == status);

            // 이미 하나에 걸려 있으면 다른 것은 걸리지 않는다.
            TestWorld.EndTurn(room, owner, other);
            ApplyType another = status == ApplyType.Burn ? ApplyType.Paralysis : ApplyType.Burn;
            other.MyPlayer.Pokemon[0].Info.Hp = MaxHp(other.MyPlayer.Pokemon[0]);
            TestWorld.Round(room, owner, TestWorld.Fight(CrunchWith(another)), other, TestWorld.Pass());
            Assert.Equal(status == ApplyType.Burn ? 1 : 0, state.Fire);
            Assert.Equal(status == ApplyType.Paralysis ? 1 : 0, state.Paralysis);
        }

        [Fact]
        public void 불꽃_타입은_화상에_걸리지_않는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            TestWorld.PutFirst(other, Infernape);

            TestWorld.Round(room, owner, TestWorld.Fight(CrunchWith(ApplyType.Burn)), other, TestWorld.Pass());

            Assert.Equal(0, other.MyPlayer.Pokemon[0].Info.State.Fire);
        }

        [Fact]
        public void 화상은_라운드가_끝날_때_최대_HP의_16분의_1을_깎는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            PokemonData burned = other.MyPlayer.Pokemon[0];
            burned.Info.State.Fire = 1;
            int hpBefore = burned.Info.Hp;

            TestWorld.Round(room, owner, TestWorld.Pass(), other, TestWorld.Pass());

            Assert.Equal(hpBefore - MaxHp(burned) / 16, burned.Info.Hp);
            Assert.Contains(ResidualPacket(owner).TurnInfo,
                info => (info.StateFlag & BattleStateFlag.DebuffFire) == BattleStateFlag.DebuffFire && info.ToId == other.MyPlayer.Id);
        }

        [Fact]
        public void 묶이면_라운드마다_8분의_1_피해를_입고_스스로_교체할_수_없다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int bindSkill = CrunchWith(ApplyType.Bind);
            PokemonData bound = other.MyPlayer.Pokemon[0];

            TestWorld.Round(room, owner, TestWorld.Fight(bindSkill), other, TestWorld.Pass());

            Assert.InRange(bound.Info.State.Bind, 3, 4);
            BattleInfo bindDamage = ResidualPacket(other).TurnInfo
                .Single(info => (info.StateFlag & BattleStateFlag.DebuffBind) == BattleStateFlag.DebuffBind);
            Assert.Equal(bindSkill, bindDamage.LatingSkillId);

            TestWorld.EndTurn(room, owner, other);
            int hpBefore = bound.Info.Hp;
            room.Turn(other.MyPlayer.Id, TestWorld.Change(Infernape));
            room.Turn(owner.MyPlayer.Id, TestWorld.Pass());
            Assert.Equal(BattlePhase.Choosing, room.Phase);
            room.Turn(other.MyPlayer.Id, TestWorld.Pass());
            Assert.Equal(Torterra, other.MyPlayer.Pokemon[0].Id);
            Assert.Equal(hpBefore - MaxHp(bound) / 8, bound.Info.Hp);

            // 묶은 포켓몬이 들어가면 풀린다.
            TestWorld.EndTurn(room, owner, other);
            TestWorld.Round(room, owner, TestWorld.Change(Infernape), other, TestWorld.Pass());
            Assert.Equal(0, bound.Info.State.Bind);
        }

        [Fact]
        public void 풀죽음은_상대보다_먼저_맞혔을_때만_상대를_막는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int flinchSkill = CrunchWith(ApplyType.Sturn);
            owner.MyPlayer.Pokemon[0].Info.Spe = 9999;
            int otherSkill = SureSeed();

            TestWorld.Round(room, owner, TestWorld.Fight(flinchSkill), other, TestWorld.Fight(otherSkill));

            S_TurnBattle flinched = FightPacket(owner, other.MyPlayer.Id);
            Assert.Contains(flinched.TurnInfo, info => (info.StateFlag & BattleStateFlag.DebuffSturn) == BattleStateFlag.DebuffSturn);
            Assert.Empty(flinched.Info);

            // 상대가 먼저 움직였으면 풀죽음은 걸리지 않고 다음 라운드로 넘어가지도 않는다.
            TestWorld.EndTurn(room, owner, other);
            owner.MyPlayer.Pokemon[0].Info.Spe = 1;
            TestWorld.Round(room, owner, TestWorld.Fight(flinchSkill), other, TestWorld.Fight(otherSkill));
            Assert.Equal(0, other.MyPlayer.Pokemon[0].Info.State.Sturn);
            Assert.NotEmpty(FightPacket(owner, other.MyPlayer.Id).Info);
        }

        [Fact]
        public void 대폭발을_쓰면_쓰러지고_다음_포켓몬을_고른_뒤_라운드가_이어진다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            // 토대부기의 기가드레인을 대폭발의 효과로 바꿔 쓴다.
            Effects(GigaDrain).Clear();
            Effects(GigaDrain).Add(Effects(Explosion));
            other.MyPlayer.Pokemon[0].Info.Hp = 99999;

            TestWorld.Round(room, owner, TestWorld.Fight(GigaDrain), other, TestWorld.Pass());
            Assert.Equal(0, owner.MyPlayer.Pokemon[0].Info.Hp);

            TestWorld.EndTurn(room, owner, other);
            Assert.Equal(BattlePhase.Replacing, room.Phase);
            Assert.True(TestWorld.Sent(owner, MsgId.STurn, S_Turn.Parser).Last().MustChange);
            Assert.False(TestWorld.Sent(other, MsgId.STurn, S_Turn.Parser).Last().MustChange);

            // 쓰러진 포켓몬으로는 고를 수 없고, 고르면 내보낸 뒤 연출을 기다린다.
            room.ChangePokemon(owner.MyPlayer.Id, Torterra);
            Assert.Equal(BattlePhase.Replacing, room.Phase);
            room.ChangePokemon(owner.MyPlayer.Id, Infernape);
            Assert.Equal(BattlePhase.Animating, room.Phase);
            Assert.Equal(Infernape, owner.MyPlayer.Pokemon[0].Id);

            TestWorld.EndTurn(room, owner, other);
            Assert.Equal(BattlePhase.Choosing, room.Phase);
        }

        [Fact]
        public void 기술을_쓰면_PP가_줄고_PP가_없으면_고를_수_없다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int pokemonId = owner.MyPlayer.Pokemon[0].Id;
            int maxPP = DataManager.SkillDict[GigaDrain].info.Pp;
            other.MyPlayer.Pokemon[0].Info.Hp = 99999;

            for (int used = 1; used <= maxPP; ++used)
            {
                TestWorld.Round(room, owner, TestWorld.Fight(GigaDrain), other, TestWorld.Pass());
                Assert.Equal(maxPP - used, room.RemainingPP(owner.MyPlayer.Id, pokemonId, GigaDrain));
                TestWorld.EndTurn(room, owner, other);
            }

            room.Turn(owner.MyPlayer.Id, TestWorld.Fight(GigaDrain));
            room.Turn(other.MyPlayer.Id, TestWorld.Pass());
            Assert.Equal(BattlePhase.Choosing, room.Phase);
        }

        [Fact]
        public void 혼란은_2에서_5턴_걸린다()
        {
            TestWorld.LoadData();
            for (int attempt = 0; attempt < 50; ++attempt)
            {
                PokemonData target = FreshPokemon(1);
                target.Info.State = new ConditionAbnormality();

                Util.CalcBattle(FreshPokemon(0), target, Effect(ApplyType.Confusion, TargetType.Enemy, 1));

                Assert.InRange(target.Info.State.Confusion, 2, 5);
            }
        }

        [Fact]
        public void 능력치를_크게_깎아도_기본값의_4분의_1_아래로_내려가지_않는다()
        {
            TestWorld.LoadData();
            PokemonData attacker = FreshPokemon(0);
            PokemonData target = FreshPokemon(1);
            int baseAtk = DataManager.PokemonDict[target.Id].Info.Atk;

            Util.CalcBattle(attacker, target, Effect(ApplyType.DebuffAtk, TargetType.Enemy, 37));

            Assert.Equal(baseAtk / 4, target.Info.Atk);
        }

        [Fact]
        public void 능력치를_크게_올려도_기본값의_4배를_넘지_않는다()
        {
            TestWorld.LoadData();
            PokemonData self = FreshPokemon(0);
            int baseSpA = DataManager.PokemonDict[self.Id].Info.SpA;

            for (int i = 0; i < 30; i++)
                Util.CalcBattle(self, self, Effect(ApplyType.BuffSpa, TargetType.Oneself, 2));

            Assert.Equal(baseSpA * 4, self.Info.SpA);
        }

        [Fact]
        public void 공격력이_깎인_포켓몬의_공격도_상대를_회복시키지_않는다()
        {
            TestWorld.LoadData();
            PokemonData weakened = FreshPokemon(0);
            PokemonData target = FreshPokemon(1);
            for (int i = 0; i < 5; i++)
                Util.CalcBattle(target, weakened, Effect(ApplyType.DebuffAtk, TargetType.Enemy, 37));
            target.Info.Hp = 10;

            Util.CalcBattle(weakened, target, Effect(ApplyType.Atk, TargetType.Enemy, 80));

            Assert.True(target.Info.Hp <= 10);
        }

        [Fact]
        public void 전투의_피해는_마스터_데이터와_상대의_같은_포켓몬에_남지_않는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int targetId = other.MyPlayer.Pokemon[0].Id;
            int maxHp = DataManager.PokemonDict[targetId].Info.Hp;

            TestWorld.Round(room, owner, TestWorld.Fight(SureHitAttack(owner)), other, TestWorld.Pass());

            Assert.True(other.MyPlayer.Pokemon[0].Info.Hp < maxHp);
            Assert.Equal(maxHp, DataManager.PokemonDict[targetId].Info.Hp);

            // 두 사람이 같은 포켓몬을 골랐어도 공격한 쪽의 같은 포켓몬은 멀쩡하다.
            PokemonData ownerSame = owner.MyPlayer.Pokemon.First(p => p.Id == targetId);
            Assert.Equal(maxHp, ownerSame.Info.Hp);
        }
    }
}
