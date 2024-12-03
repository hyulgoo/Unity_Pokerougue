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

        public static BattleInfo[] CalcBattle(PokemonData myData, PokemonData enemyData, SkillEffect effect)
        {
            System.Random r = new System.Random();
            bool isMiss = r.Next(0, 99) >= effect.Accuracy ? true : false;

            ApplyType applyType = effect.ApplyType;

            BattleInfo[] result = new BattleInfo[(int)TargetType.End];
            // 빗나갔다면 바로 return
            if (isMiss)
            {
                for (int i = 0; i < (int)TargetType.End; ++i)
                {
                    result[i] = new BattleInfo();
                    result[i].StateInfo = new StateInfo();
                    result[i].ApplyType = applyType;
                    result[i].TargetType = effect.Target;
                    result[i].Effective = EffectiveType.Commoneffect;
                    result[i].FromData= i == (int)TargetType.Oneself ? myData.Clone() : myData.Clone();
                    result[i].ToData = i == (int)TargetType.Oneself ? enemyData.Clone() : myData.Clone();
                    result[i].StateInfo.IsMiss = isMiss;
                }
                return result;
            }

            PokemonData targetData = effect.Target == TargetType.Oneself ? myData : enemyData;
            PokemonInfo targetInfo = targetData.Info;
            PokemonInfo targetStandard = DataManager.PokemonDict[targetData.Id].Info;
            EffectiveType effective = EffectiveType.Commoneffect;
            // 스킬타입이 공격 또는 특수공격인 경우 데미지 계산
            if (applyType == ApplyType.Atk || applyType == ApplyType.Spa)
            {
                string attackerName = myData.Name;
                string targetName = targetData.Name;
                int Damege = CalcDamage(effect, myData.Info, targetInfo, out effective);
                targetInfo.Hp -= Damege;
            }
            else // 스킬타입이 공격타입이 아닌 경우
            {
                CalcBuffType(targetInfo, targetStandard, applyType, effect.Value);
            }

            // 데미지를 계산한 이후 결과를 반영해줌.
            for (int i = 0; i < (int)TargetType.End; ++i)
            {
                result[i] = new BattleInfo();
                result[i].StateInfo = new StateInfo();
                result[i].ApplyType = applyType;
                result[i].TargetType = effect.Target;
                result[i].Effective = effective;
                result[i].StateInfo.IsMiss = isMiss;
                result[i].FromData = myData.Clone();
                result[i].ToData = targetData.Clone();
            }

            return result;
        }

        static int CalcDamage(SkillEffect skillInfo, PokemonInfo attackerInfo, PokemonInfo targetInfo, out EffectiveType effective)
        {
            System.Random r = new System.Random();

            int attackValue = skillInfo.ApplyType == ApplyType.Atk ? attackerInfo.Atk : attackerInfo.SpA;
            int defenseValue = skillInfo.ApplyType == ApplyType.Atk ? targetInfo.Def : targetInfo.SpD;

            // 스킬 위력계수
            int skillPower = skillInfo.Value;

            // 6.5% 확률로 크리티컬 판정
            int criticalRatio = r.Next(0, 1000) < 65 ? 2 : 1;

            // 랜덤 데미지계수
            int randomRatio = (r.Next(217, 256) * 100) / 255;

            // 자속성 기술계수
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

            effective = EffectiveType.Commoneffect;
            if (effectRatio != 1f)
                effective = effectRatio > 1f ? EffectiveType.Effective : EffectiveType.Ineffective;
            
            damage *= Mode3;

            if(damage > targetInfo.Hp)
                damage = targetInfo.Hp;

            return (int)damage;
        }

        static void CalcBuffType(PokemonInfo targetInfo, PokemonInfo stadardInfo, ApplyType skillType, int value)
        {
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
