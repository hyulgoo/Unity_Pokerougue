using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Server.Data;
using Type = Google.Protobuf.Protocol.Type;

namespace Server
{
    internal class Util
    {
        public static float Mod1 { get; set; } = 1f;
        public static float Mod2 { get; set; } = 1f;
        public static float Mode3 { get; set; } = 1f;

        #region CalcTypeRatio

        public static float CalcAttackType(Type type, Type opponent)
        {
            float damageRatio = 1f;
            switch (type)
            {
                case Type.Normal:
                    if (opponent == Type.Rock || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Ghost)
                        damageRatio = 0f;
                    break;
                case Type.Fighting:
                    if (opponent == Type.Normal || opponent == Type.Ice
                                                || opponent == Type.Rock || opponent == Type.Dark
                                                || opponent == Type.Steel)
                        damageRatio = 2f;
                    else if (opponent == Type.Poison || opponent == Type.Flying
                                                     || opponent == Type.Psychic || opponent == Type.Bug
                                                     || opponent == Type.Fairy)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Ghost)
                        damageRatio = 0f;
                    break;
                case Type.Flying:
                    if (opponent == Type.Grass || opponent == Type.Fighting
                                               || opponent == Type.Bug)
                        damageRatio = 2f;
                    else if (opponent == Type.Electric || opponent == Type.Rock
                                                       || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    break;
                case Type.Poison:
                    if (opponent == Type.Grass || opponent == Type.Fairy)
                        damageRatio = 2f;
                    else if (opponent == Type.Poison || opponent == Type.Ground
                                                     || opponent == Type.Rock || opponent == Type.Ghost)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Steel)
                        damageRatio = 0f;
                    break;
                case Type.Ground:
                    if (opponent == Type.Fire || opponent == Type.Electric
                                              || opponent == Type.Poison || opponent == Type.Rock
                                              || opponent == Type.Steel)
                        damageRatio = 2f;
                    else if (opponent == Type.Grass || opponent == Type.Bug)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Flying)
                        damageRatio = 0f;
                    break;
                case Type.Rock:
                    if (opponent == Type.Fire || opponent == Type.Ice
                                              || opponent == Type.Flying || opponent == Type.Bug)
                        damageRatio = 2f;
                    else if (opponent == Type.Fighting || opponent == Type.Ground
                                                       || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    break;
                case Type.Steel:
                    if (opponent == Type.Ice || opponent == Type.Rock
                                             || opponent == Type.Fairy)
                        damageRatio = 2f;
                    else if (opponent == Type.Fire || opponent == Type.Water
                                                   || opponent == Type.Electric || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    break;
                case Type.Bug:
                    if (opponent == Type.Grass || opponent == Type.Psychic
                                               || opponent == Type.Dark)
                        damageRatio = 2f;
                    else if (opponent == Type.Fire || opponent == Type.Fighting
                                                   || opponent == Type.Poison || opponent == Type.Flying
                                                   || opponent == Type.Ghost || opponent == Type.Steel
                                                   || opponent == Type.Fairy)
                        damageRatio = 0.5f;
                    break;
                case Type.Ghost:
                    if (opponent == Type.Psychic || opponent == Type.Ghost
                                                 || opponent == Type.Flying || opponent == Type.Bug)
                        damageRatio = 2f;
                    else if (opponent == Type.Dark)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Normal)
                        damageRatio = 0f;
                    break;
                case Type.Fire:
                    if (opponent == Type.Grass || opponent == Type.Ice
                                               || opponent == Type.Bug || opponent == Type.Steel)
                        damageRatio = 2f;
                    else if (opponent == Type.Fire || opponent == Type.Water
                                                   || opponent == Type.Rock || opponent == Type.Dragon)
                        damageRatio = 0.5f;
                    break;
                case Type.Water:
                    if (opponent == Type.Fire || opponent == Type.Ground
                                              || opponent == Type.Rock)
                        damageRatio = 2f;
                    else if (opponent == Type.Water || opponent == Type.Grass
                                                    || opponent == Type.Dragon)
                        damageRatio = 0.5f;
                    break;
                case Type.Grass:
                    if (opponent == Type.Water || opponent == Type.Ground
                                               || opponent == Type.Rock)
                        damageRatio = 2f;
                    else if (opponent == Type.Fire || opponent == Type.Grass
                                                   || opponent == Type.Poison || opponent == Type.Flying
                                                   || opponent == Type.Bug || opponent == Type.Dragon
                                                   || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    break;
                case Type.Electric:
                    if (opponent == Type.Water || opponent == Type.Flying)
                        damageRatio = 2f;
                    else if (opponent == Type.Grass || opponent == Type.Electric
                                                    || opponent == Type.Dragon)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Ground)
                        damageRatio = 0f;
                    break;
                case Type.Psychic:
                    if (opponent == Type.Fighting || opponent == Type.Poison)
                        damageRatio = 2f;
                    else if (opponent == Type.Psychic || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Dark)
                        damageRatio = 0f;
                    break;
                case Type.Ice:
                    if (opponent == Type.Grass || opponent == Type.Ground
                                               || opponent == Type.Flying || opponent == Type.Dragon)
                        damageRatio = 2f;
                    else if (opponent == Type.Fire || opponent == Type.Water
                                                   || opponent == Type.Ice || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    break;
                case Type.Dragon:
                    if (opponent == Type.Dragon)
                        damageRatio = 2f;
                    else if (opponent == Type.Steel)
                        damageRatio = 0.5f;
                    else if (opponent == Type.Fairy)
                        damageRatio = 0f;
                    break;
                case Type.Dark:
                    if (opponent == Type.Psychic || opponent == Type.Ghost)
                        damageRatio = 2f;
                    else if (opponent == Type.Fighting || opponent == Type.Dark
                                                       || opponent == Type.Fairy)
                        damageRatio = 0.5f;
                    break;
                case Type.Fairy:
                    if (opponent == Type.Fighting || opponent == Type.Dragon
                                                  || opponent == Type.Dark)
                        damageRatio = 2f;
                    else if (opponent == Type.Fire || opponent == Type.Poison
                                                   || opponent == Type.Steel)
                        damageRatio = 0.5f;
                    break;
            }

            return damageRatio;
        }

        #endregion

        // lastDamage: 같은 기술의 앞선 효과가 준 피해. 흡수(Recovery) 효과가 이 값의 value%를 회복한다.
        public static BattleInfo CalcBattle(PokemonData myData, PokemonData enemyData, SkillEffect effect, int lastDamage = 0)
        {
            Random random = new Random();
            bool isMiss = random.Next(0, 101) > effect.Accuracy ? true : false;
            bool isCritical = random.Next(0, 1000) < 65 ? true : false;

            BattleInfo battleInfo = new BattleInfo();
            battleInfo.ApplyType = effect.ApplyType;
            battleInfo.TargetType = effect.Target;
            battleInfo.StateFlag = BattleStateFlag.Default;

            if (isMiss && effect.Target == TargetType.Enemy)
            {
                // 클라이언트가 FromData로 "~의 공격은 빗나갔다"를 만든다.
                battleInfo.FromData = myData.Clone();
                battleInfo.ToData = enemyData.Clone();
                battleInfo.StateFlag = battleInfo.StateFlag |= BattleStateFlag.Miss;
                return battleInfo;
            }

            PokemonData targetData = effect.Target == TargetType.Oneself ? myData : enemyData;
            bool isAttack = effect.ApplyType == ApplyType.Atk || effect.ApplyType == ApplyType.Spa;
            if (isAttack && effect.Target == TargetType.Oneself)
            {
                // 반동: 이 기술로 준 피해의 value%(최소 1)를 자신이 입는다. 급소·상성과 무관하다.
                int recoil = lastDamage > 0 ? Math.Max(1, lastDamage * effect.Value / 100) : 0;
                targetData.Info.Hp = Math.Max(0, targetData.Info.Hp - recoil);
                isCritical = false;
            }
            else if (effect.ApplyType == ApplyType.Dot && targetData.Info.Type.Contains(Type.Grass))
            {
                // 풀 타입에게는 씨뿌리기가 듣지 않는다.
                battleInfo.StateFlag |= BattleStateFlag.Noneeffective;
                isCritical = false;
            }
            else if (isAttack)
            {
                float effectRatio = CalcDamageAndReturnEffective(effect, myData.Info, targetData.Info, isCritical);
                if (effectRatio < 1f)
                    battleInfo.StateFlag |= BattleStateFlag.Ineffective;
                else if (effectRatio > 1f)
                    battleInfo.StateFlag |= BattleStateFlag.Effective;
                else if (effectRatio == 0f)
                    battleInfo.StateFlag |= BattleStateFlag.Noneeffective;
            }
            else
            {
                CalcBuffType(targetData, effect.ApplyType, effect.Value, battleInfo.StateFlag, lastDamage);
            }

            if (isCritical)
                battleInfo.StateFlag |= BattleStateFlag.Critical;
            battleInfo.FromData = myData.Clone();
            battleInfo.ToData = targetData.Clone();
            battleInfo.SkillValue = effect.Value;

            return battleInfo;
        }

        private static float CalcDamageAndReturnEffective(SkillEffect skillInfo, PokemonInfo attackerInfo,
            PokemonInfo targetInfo, bool isCritical)
        {
            Random random = new Random();

            int attackValue = skillInfo.ApplyType == ApplyType.Atk ? attackerInfo.Atk : attackerInfo.SpA;
            int defenseValue = skillInfo.ApplyType == ApplyType.Atk ? targetInfo.Def : targetInfo.SpD;
            int skillPower = skillInfo.Value;
            int criticalRatio = isCritical ? 2 : 1;
            int randomRatio = random.Next(217, 256) * 100 / 255;

            float myType = 1f;
            foreach (Type type in attackerInfo.Type)
                if (type == skillInfo.Type)
                    myType *= 1.5f;

            float damage =
                ((attackerInfo.Level * 2 / 5 + 2) * skillPower * attackValue / 50 / defenseValue * Mod1 + 2) *
                criticalRatio * Mod2 * (randomRatio / 100f) * myType;

            float effectRatio = 1f;
            foreach (Type type in targetInfo.Type)
            {
                float ratio = CalcAttackType(skillInfo.Type, type);
                effectRatio *= ratio;
            }

            damage *= effectRatio;
            damage *= Mode3;

            // 화상을 입은 포켓몬의 물리 공격은 위력이 절반이 된다.
            if (skillInfo.ApplyType == ApplyType.Atk && attackerInfo.State != null && attackerInfo.State.Fire > 0)
                damage *= 0.5f;

            // 계산이 어떻게 나오든 공격이 상대를 회복시키지 않게 한다.
            int dealtDamage = Math.Max(0, (int)damage);
            targetInfo.Hp = dealtDamage >= targetInfo.Hp ? 0 : targetInfo.Hp - dealtDamage;

            return effectRatio;
        }

        // 능력치 변화는 한 단계에 기본값의 1/5씩, 기본값의 1/4 ~ 4배를 넘지 않는다.
        // 막지 않으면 공격력이 음수가 되어 데미지가 상대를 회복시킨다.
        private static int ChangeStat(int current, int standard, int stages)
        {
            int changed = current + standard / 5 * stages;
            return Math.Clamp(changed, Math.Max(1, standard / 4), standard * 4);
        }

        private static void CalcBuffType(PokemonData targetData, ApplyType skillType, int value,
            BattleStateFlag battleFlag, int lastDamage)
        {
            PokemonInfo targetInfo = targetData.Info;
            PokemonInfo stadardInfo = DataManager.PokemonDict[targetData.Id].Info;

            switch (skillType)
            {
                case ApplyType.Recovery:
                    // 흡수: 이 기술로 준 피해의 value%를 회복한다(기가드레인 50). 피해를 줬다면 최소 1.
                    int drain = lastDamage * value / 100;
                    if (lastDamage > 0 && drain < 1)
                        drain = 1;
                    targetInfo.Hp += drain;
                    if (targetInfo.Hp > stadardInfo.Hp)
                        targetInfo.Hp = stadardInfo.Hp;
                    break;
                case ApplyType.BuffAtk:
                    targetInfo.Atk = ChangeStat(targetInfo.Atk, stadardInfo.Atk, value);
                    break;
                case ApplyType.BuffSpa:
                    targetInfo.SpA = ChangeStat(targetInfo.SpA, stadardInfo.SpA, value);
                    break;
                case ApplyType.BuffDef:
                    targetInfo.Def = ChangeStat(targetInfo.Def, stadardInfo.Def, value);
                    break;
                case ApplyType.BuffSpd:
                    targetInfo.SpD = ChangeStat(targetInfo.SpD, stadardInfo.SpD, value);
                    break;
                case ApplyType.BuffSpe:
                    targetInfo.Spe = ChangeStat(targetInfo.Spe, stadardInfo.Spe, value);
                    break;
                case ApplyType.Dot:
                    // 씨뿌리기는 교체될 때까지 계속된다(교체 시 GameRoom.ChangePokemon이 푼다).
                    targetInfo.State.Dot = int.MaxValue;
                    break;
                case ApplyType.DebuffAtk:
                    targetInfo.Atk = ChangeStat(targetInfo.Atk, stadardInfo.Atk, -value);
                    break;
                case ApplyType.DebuffSpa:
                    targetInfo.SpA = ChangeStat(targetInfo.SpA, stadardInfo.SpA, -value);
                    break;
                case ApplyType.DebuffDef:
                    targetInfo.Def = ChangeStat(targetInfo.Def, stadardInfo.Def, -value);
                    break;
                case ApplyType.DebuffSpd:
                    targetInfo.SpD = ChangeStat(targetInfo.SpD, stadardInfo.SpD, -value);
                    break;
                case ApplyType.DebuffSpe:
                    targetInfo.Spe = ChangeStat(targetInfo.Spe, stadardInfo.Spe, -value);
                    break;
                case ApplyType.Confusion:
                    // 혼란은 2~5턴. 이미 혼란이면 다시 걸리지 않는다.
                    if (targetInfo.State.Confusion == 0)
                        targetInfo.State.Confusion = Random.Shared.Next(2, 6);
                    break;
            }
        }

        public static void AddRepeatedFieldToList(RepeatedField<int> targetList, List<int> list)
        {
            for (int index = 0; index < list.Count; index++)
                targetList.Add(list[index]);
        }

        public static void AddRepeatedFieldToList(RepeatedField<BattleInfo> targetList, List<BattleInfo> list)
        {
            for (int index = 0; index < list.Count; index++)
                targetList.Add(list[index]);
        }
    }
}