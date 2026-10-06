using System;
using System.Linq;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Room;
using Server.Packet;
using Xunit;

namespace Server.Tests
{
    // 전투방에서의 행위자·턴·스킬·선택 검증 (PK-R05, PK-R13~R16)
    public class BattleAuthorityTests
    {
        private static (GameRoom room, ClientSession a, ClientSession b) EnterBattleRoom()
        {
            TestWorld.LoadData();
            GameRoom lobby = GameLogic.Instance.Add();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            ClientSession b = TestWorld.EnterLobby(lobby, "b");
            return (TestWorld.StartDuel(lobby, a, b), a, b);
        }

        private static int OwnedSkill(ClientSession session)
        {
            return session.MyPlayer.Pokemon[0].Info.SkillId[0];
        }

        [Fact]
        public void 데이터의_포켓몬은_스킬_목록을_갖고_있다()
        {
            TestWorld.LoadData();

            Assert.All(DataManager.PokemonDict.Values, pokemon => Assert.NotEmpty(pokemon.Info.SkillId));
        }

        [Fact]
        public void 포켓몬을_한_마리만_고르면_선택이_거절된다()
        {
            (GameRoom room, ClientSession a, _) = EnterBattleRoom();

            room.SelectPokemon(a.MyPlayer.Id, TestWorld.Select(TestWorld.TwoPokemon()[0]));

            Assert.Empty(a.MyPlayer.Pokemon);
        }

        [Fact]
        public void 같은_포켓몬을_두_번_고르면_선택이_거절된다()
        {
            (GameRoom room, ClientSession a, _) = EnterBattleRoom();
            int id = TestWorld.TwoPokemon()[0];

            room.SelectPokemon(a.MyPlayer.Id, TestWorld.Select(id, id));

            Assert.Empty(a.MyPlayer.Pokemon);
        }

        [Fact]
        public void 데이터에_없는_포켓몬을_고르면_예외_없이_거절된다()
        {
            (GameRoom room, ClientSession a, _) = EnterBattleRoom();

            Exception exception = Record.Exception(() =>
                room.SelectPokemon(a.MyPlayer.Id, TestWorld.Select(TestWorld.TwoPokemon()[0], 123456)));

            Assert.Null(exception);
            Assert.Empty(a.MyPlayer.Pokemon);
        }

        [Fact]
        public void 선택_패킷의_PlayerId를_상대로_적어도_보낸_사람의_선택으로_처리된다()
        {
            (GameRoom room, ClientSession a, ClientSession b) = EnterBattleRoom();
            C_SelectPokemon packet = TestWorld.Select(TestWorld.TwoPokemon());
            packet.PlayerId = b.MyPlayer.Id;

            PacketHandler.C_SelectPokemonHandler(a, packet);
            TestWorld.Pump();

            Assert.Equal(2, a.MyPlayer.Pokemon.Count);
            Assert.Empty(b.MyPlayer.Pokemon);
        }

        [Fact]
        public void 두_사람이_두_마리씩_고르면_전투가_시작되고_다시_고를_수_없다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int[] before = owner.MyPlayer.GetPokemonList();

            room.SelectPokemon(owner.MyPlayer.Id, TestWorld.Select(before.Reverse().ToArray()));

            Assert.NotEqual(0, room.CurrentTurnPlayerId);
            Assert.Equal(2, other.MyPlayer.Pokemon.Count);
            Assert.Equal(before, owner.MyPlayer.GetPokemonList());
        }

        [Fact]
        public void 전투가_시작되기_전의_턴_패킷은_예외_없이_무시된다()
        {
            (GameRoom room, ClientSession a, ClientSession b) = EnterBattleRoom();

            Exception exception = Record.Exception(() =>
            {
                room.Turn(a.MyPlayer.Id, TestWorld.Fight(1));
                room.Turn(a.MyPlayer.Id, TestWorld.Runaway());
                room.TurnEnd(a.MyPlayer.Id);
                room.ChangePokemon(a.MyPlayer.Id, 1);
            });

            Assert.Null(exception);
            Assert.Equal(PlayerServerState.ServerStateGame, a.ServerState);
            Assert.Equal(PlayerServerState.ServerStateGame, b.ServerState);
        }

        [Fact]
        public void 턴_주인의_공격_뒤_양쪽이_턴_종료를_보내면_턴이_넘어간다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();

            room.Turn(owner.MyPlayer.Id, TestWorld.Fight(OwnedSkill(owner)));
            TestWorld.EndTurn(room, owner, other);

            Assert.Equal(other.MyPlayer.Id, room.CurrentTurnPlayerId);
        }

        [Fact]
        public void 상대_턴에_보낸_공격은_무시된다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int ownerHp = owner.MyPlayer.Pokemon[0].Info.Hp;

            room.Turn(other.MyPlayer.Id, TestWorld.Fight(OwnedSkill(other)));
            TestWorld.EndTurn(room, owner, other);

            // 턴이 진행되지 않았으므로 턴 종료도 받지 않고, 턴 주인이 그대로다.
            Assert.Equal(owner.MyPlayer.Id, room.CurrentTurnPlayerId);
            Assert.Equal(ownerHp, owner.MyPlayer.Pokemon[0].Info.Hp);
        }

        [Fact]
        public void 한_턴에_행동을_두_번_보내면_두_번째는_무시된다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();

            room.Turn(owner.MyPlayer.Id, TestWorld.Fight(OwnedSkill(owner)));
            room.Turn(owner.MyPlayer.Id, TestWorld.Runaway());

            // 두 번째 행동(도주)이 처리됐다면 양쪽이 로비 상태로 돌아갔을 것이다.
            Assert.Equal(PlayerServerState.ServerStateGame, owner.ServerState);
            Assert.Equal(PlayerServerState.ServerStateGame, other.ServerState);
        }

        [Fact]
        public void 갖고_있지_않은_스킬과_없는_스킬은_거절된다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int notOwned = DataManager.SkillDict.Keys
                .First(id => !owner.MyPlayer.Pokemon[0].Info.SkillId.Contains(id));

            Exception exception = Record.Exception(() =>
            {
                room.Turn(owner.MyPlayer.Id, TestWorld.Fight(notOwned));
                room.Turn(owner.MyPlayer.Id, TestWorld.Fight(123456));
                room.Turn(owner.MyPlayer.Id, TestWorld.Fight(-1));
            });
            TestWorld.EndTurn(room, owner, other);

            Assert.Null(exception);
            Assert.Equal(owner.MyPlayer.Id, room.CurrentTurnPlayerId);

            // 거절된 뒤에도 턴이 살아 있어 올바른 스킬은 쓸 수 있다.
            room.Turn(owner.MyPlayer.Id, TestWorld.Fight(OwnedSkill(owner)));
            TestWorld.EndTurn(room, owner, other);
            Assert.Equal(other.MyPlayer.Id, room.CurrentTurnPlayerId);
        }

        [Fact]
        public void 상대의_ID를_적은_도주_패킷으로_상대를_지게_만들_수_없다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();

            // 턴이 아닌 쪽이 "턴 주인이 도주했다"는 패킷을 보낸다. 행위자는 보낸 세션이다.
            PacketHandler.C_TurnHandler(other, TestWorld.Runaway(owner.MyPlayer.Id));
            TestWorld.Pump();

            Assert.Equal(PlayerServerState.ServerStateGame, owner.ServerState);
            Assert.Equal(PlayerServerState.ServerStateGame, other.ServerState);
        }

        [Fact]
        public void 자기_턴의_도주는_전투를_끝내고_그_뒤의_턴_패킷은_무시된다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();

            PacketHandler.C_TurnHandler(owner, TestWorld.Runaway());
            TestWorld.Pump();

            Assert.Equal(PlayerServerState.ServerStateLobby, owner.ServerState);
            Assert.Equal(PlayerServerState.ServerStateLobby, other.ServerState);

            Exception exception = Record.Exception(() =>
                room.Turn(other.MyPlayer.Id, TestWorld.Fight(OwnedSkill(other))));
            Assert.Null(exception);
        }

        [Fact]
        public void 방에_없는_ID의_턴_종료는_턴_진행을_막지_않는다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            room.Turn(owner.MyPlayer.Id, TestWorld.Fight(OwnedSkill(owner)));

            room.TurnEnd(123456);
            TestWorld.EndTurn(room, owner, other);

            Assert.Equal(other.MyPlayer.Id, room.CurrentTurnPlayerId);
        }

        [Fact]
        public void 자기_목록에_없는_포켓몬으로는_교체되지_않는다()
        {
            (GameRoom room, ClientSession owner, _) = TestWorld.StartBattle();
            int[] before = owner.MyPlayer.GetPokemonList();

            Exception exception = Record.Exception(() => room.ChangePokemon(owner.MyPlayer.Id, 123456));

            Assert.Null(exception);
            Assert.Equal(before, owner.MyPlayer.GetPokemonList());
        }

        [Fact]
        public void 교체_패킷의_PlayerId를_상대로_적어도_보낸_사람의_포켓몬이_바뀐다()
        {
            (GameRoom room, ClientSession owner, ClientSession other) = TestWorld.StartBattle();
            int[] ownerBefore = owner.MyPlayer.GetPokemonList();
            int[] otherBefore = other.MyPlayer.GetPokemonList();

            PacketHandler.C_ChangePokemonHandler(other,
                new C_ChangePokemon { PlayerId = owner.MyPlayer.Id, ChangePokemonId = otherBefore[1] });
            TestWorld.Pump();

            Assert.Equal(ownerBefore, owner.MyPlayer.GetPokemonList());
            Assert.Equal(otherBefore.Reverse(), other.MyPlayer.GetPokemonList());
        }
    }
}
