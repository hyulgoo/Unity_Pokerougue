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

        public static BattleInfo CalcDamage(PokemonData myData, PokemonData enemyData, SkillEffect effect)
        {
            System.Random r = new System.Random();
            bool isMiss = r.Next(0, 99) >= effect.Accuracy ? true : false;

            PokemonInfo myInfo = myData.info;
            PokemonInfo enemyInfo = enemyData.info;
            float damage = 0;
            SkillType skillType = effect.SkillType;

            BattleInfo result = new BattleInfo();
            result.StateInfo = new StateInfo();
            result.SkillType = effect.SkillType;
            result.MyInfo = myInfo;
            result.EnemyInfo = enemyInfo;
            result.StateInfo.IsMiss = isMiss;

            // 빗나갔다면 바로 return
            if (isMiss)
                return result;

            PokemonInfo info = myInfo;
            PokemonInfo target = effect.Target == TargetType.Oneself ? myInfo : enemyInfo;
;
            int targetId = effect.Target == TargetType.Oneself ? myData.id : enemyData.id;
            PokemonInfo standard = DataManager.MonsterDict[targetId].info;

            int power = effect.Value;            
            int critical = r.Next(0, 1000) < 65 ? 2 : 1;
            int random = (r.Next(217, 256) * 100) / 255;
            float myType = 1f;
            for(int t = 0; t < myInfo.Type.Count; ++t)
            {
                if (myInfo.Type[0] == effect.Type)
                    myType = 1.5f;
            }
            // 스킬타입이 공격 또는 특수공격인 경우 데미지 계산
            if (skillType == SkillType.Atk || skillType == SkillType.Spa)
            {
                int atk = skillType == SkillType.Atk ? info.Atk : info.SpA;
                int def = skillType == SkillType.Atk ? target.Def: target.SpD;
                damage = (((((((myInfo.Level * 2 / 5) + 2) * power * atk / 50) / def) * Mod1) + 2) * critical * Mod2 * random / 100) * myType;
                for (int j = 0; j < target.Type.Count; ++j)
                {
                    damage *= CalcAttackType(effect.Type, target.Type[j]);
                }
                damage *= Mode3;
                int idamage = (int)damage;
                target.Hp = target.Hp - idamage < 0 ? 0 : target.Hp - idamage;
            }
            // 스킬타입이 공격타입이 아닌 경우
            else
            {
                switch (skillType)
                {
                    case SkillType.Recovery:
                        info.Hp += standard.Hp / 3;
                        break;
                    case SkillType.BuffAtk:
                        info.Atk = info.Atk + (standard.Atk / 5 * effect.Value);
                        break;
                    case SkillType.BuffSpa:
                        info.SpA = info.SpA + (standard.SpA / 5 * effect.Value);
                        break;
                    case SkillType.BuffDef:
                        info.Def = info.Def + (standard.Def / 5 * effect.Value);
                        break;
                    case SkillType.BuffSpd:
                        info.SpD = info.SpD + (standard.SpD / 5 * effect.Value);
                        break;
                    case SkillType.BuffSpe:
                        info.Spe = info.Spe + (standard.Spe / 5 * effect.Value);
                        break;
                    case SkillType.Dot:
                        info.State.Dot = 3;
                        break;
                    case SkillType.DebuffAtk:
                        info.Atk = info.Atk - (standard.Atk / 5 * effect.Value);
                        break;
                    case SkillType.DebuffSpa:
                        info.SpA = info.SpA - (standard.SpA / 5 * effect.Value);
                        break;
                    case SkillType.DebuffDef:
                        info.Def = info.Def - (standard.Def / 5 * effect.Value);
                        break;
                    case SkillType.DebuffSpd:
                        info.SpD = info.SpD - (standard.SpD / 5 * effect.Value);
                        break;
                    case SkillType.DebuffSpe:
                        info.Spe = info.Spe - (standard.Spe / 5 * effect.Value);
                        break;
                    case SkillType.Sturn:
                        info.State.Sturn = 3;
                        break;
                    case SkillType.Confusion:
                        info.State.Confusion = 3;
                        break;
                }
            }

            if (effect.Target == TargetType.Oneself)
                result.MyInfo = info;
            else
                result.EnemyInfo = info;

            return result;
        }

        public static void AddtoTargetList(RepeatedField<int> targetList, List<int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                targetList.Add(list[i]);
            }
        }

        public static void AddtoTargetList(RepeatedField<BattleInfo> targetList, List< BattleInfo> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                targetList.Add(list[i]);
            }
        }
    }
}
