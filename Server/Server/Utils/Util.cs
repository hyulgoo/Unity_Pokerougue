using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System.Collections.Generic;

namespace Server
{
    class Util
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

        public static RepeatedField<BattleInfo> CalcBattle(PokemonData myData, PokemonData enemyData, SkillEffect effect)
        {
            RepeatedField<BattleInfo> result = new RepeatedField<BattleInfo>();

            System.Random random = new System.Random();
            bool isMiss = random.Next(0, 101) > effect.Accuracy ? true : false;
            bool isCritical = random.Next(0, 1000) < 65 ? true : false;

            if (isMiss)
            {
                BattleInfo missBattleInfo = new BattleInfo();
                missBattleInfo.ApplyType = effect.ApplyType;
                missBattleInfo.TargetType = effect.Target;
                missBattleInfo.StateFlag = BattleStateFlag.Default;
                missBattleInfo.FromData = myData.Clone();
                missBattleInfo.ToData = effect.Target == (int)TargetType.Oneself ? myData.Clone() : enemyData.Clone();
                missBattleInfo.StateFlag = isMiss ? missBattleInfo.StateFlag |= BattleStateFlag.Miss : missBattleInfo.StateFlag &= ~BattleStateFlag.Miss;
                result.Add(missBattleInfo);

                return result;
            }

            // 데미지를 계산한 이후 결과를 반영해줌.
            BattleInfo battleInfo = new BattleInfo();

            PokemonData targetData = effect.Target == TargetType.Oneself ? myData : enemyData;

            if (effect.ApplyType == ApplyType.Atk || effect.ApplyType == ApplyType.Spa)
                CalcDamageAndReturnEffective(effect, myData.Info, targetData.Info, isCritical, battleInfo.StateFlag);
            else 
                CalcBuffType(targetData, effect.ApplyType, effect.Value);

            battleInfo.ApplyType = effect.ApplyType;
            battleInfo.TargetType = effect.Target;
            battleInfo.StateFlag = isMiss ? battleInfo.StateFlag |= BattleStateFlag.Miss : battleInfo.StateFlag &= ~BattleStateFlag.Miss;
            battleInfo.StateFlag = isCritical ? battleInfo.StateFlag |= BattleStateFlag.Critical : battleInfo.StateFlag &= ~BattleStateFlag.Critical;
            battleInfo.FromData = myData.Clone();
            battleInfo.ToData = targetData.Clone();
            battleInfo.SkillValue = effect.Value;
            result.Add(battleInfo);

            return result;
        }

        static void CalcDamageAndReturnEffective(SkillEffect skillInfo, PokemonInfo attackerInfo, PokemonInfo targetInfo, bool isCritical, BattleStateFlag battleStateFlag)
        {
            System.Random r = new System.Random();

            int attackValue = skillInfo.ApplyType == ApplyType.Atk ? attackerInfo.Atk : attackerInfo.SpA;
            int defenseValue = skillInfo.ApplyType == ApplyType.Atk ? targetInfo.Def : targetInfo.SpD;
            int skillPower = skillInfo.Value;
            int criticalRatio = isCritical ? 2 : 1;
            int randomRatio = (r.Next(217, 256) * 100) / 255;

            float myType = 1f;
            foreach(Type type in attackerInfo.Type)
            {
                if (type == skillInfo.Type)
                    myType *= 1.5f;
            }

            float damage = (((((((attackerInfo.Level * 2 / 5) + 2) * skillPower * attackValue / 50) / defenseValue) * Mod1) + 2) * criticalRatio * Mod2 * ((float)randomRatio / 100f)) * myType;

            float effectRatio = 1f;
            foreach (Type type in targetInfo.Type)
            {
                float ratio = CalcAttackType(skillInfo.Type, type);
                effectRatio *= ratio;
            }

            damage *= effectRatio;

            if (effectRatio < 1f)
                battleStateFlag |= BattleStateFlag.Ineffective;
            else if (effectRatio > 1f)
                battleStateFlag |= BattleStateFlag.Effective;

            damage *= Mode3;

            targetInfo.Hp = (int)damage >= targetInfo.Hp ? 0 : targetInfo.Hp - (int)damage;
        }

        static void CalcBuffType(PokemonData targetData, ApplyType skillType, int value)
        {
            PokemonInfo targetInfo = targetData.Info;
            PokemonInfo stadardInfo = DataManager.PokemonDict[targetData.Id].Info;

            switch (skillType)
            {
                case ApplyType.Recovery:
                    targetInfo.Hp += stadardInfo.Hp / 3;
                    break;
                case ApplyType.BuffAtk:
                    targetInfo.Atk = targetInfo.Atk + (stadardInfo.Atk / 5 * value);
                    break;
                case ApplyType.BuffSpa:
                    targetInfo.SpA = targetInfo.SpA + (stadardInfo.SpA / 5 * value);
                    break;
                case ApplyType.BuffDef:
                    targetInfo.Def = targetInfo.Def + (stadardInfo.Def / 5 * value);
                    break;
                case ApplyType.BuffSpd:
                    targetInfo.SpD = targetInfo.SpD + (stadardInfo.SpD / 5 * value);
                    break;
                case ApplyType.BuffSpe:
                    targetInfo.Spe = targetInfo.Spe + (stadardInfo.Spe / 5 * value);
                    break;
                case ApplyType.Dot:
                    targetInfo.State.Dot = 3;
                    break;
                case ApplyType.DebuffAtk:
                    targetInfo.Atk = targetInfo.Atk - (stadardInfo.Atk / 5 * value);
                    break;
                case ApplyType.DebuffSpa:
                    targetInfo.SpA = targetInfo.SpA - (stadardInfo.SpA / 5 * value);
                    break;
                case ApplyType.DebuffDef:
                    targetInfo.Def = targetInfo.Def - (stadardInfo.Def / 5 * value);
                    break;
                case ApplyType.DebuffSpd:
                    targetInfo.SpD = targetInfo.SpD - (stadardInfo.SpD / 5 * value);
                    break;
                case ApplyType.DebuffSpe:
                    targetInfo.Spe = targetInfo.Spe - (stadardInfo.Spe / 5 * value);
                    break;
                case ApplyType.Sturn:
                    targetInfo.State.Sturn = 3;
                    break;
                case ApplyType.Confusion:
                    targetInfo.State.Confusion = 3;
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
