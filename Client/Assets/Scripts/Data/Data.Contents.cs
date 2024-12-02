using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data
{
    #region Skill
    [Serializable]
    public class SkillData
    {
        public int id;
        public string name;
        public SkillInfo info;
    }

    [Serializable]
    public class SkillLoader : ILoader<int, SkillData>
    {
        public List<SkillData> skills = new List<SkillData>();

        public Dictionary<int, SkillData> MakeDict()
        {
            Dictionary<int, SkillData> dict = new Dictionary<int, SkillData>();
            foreach (SkillData skill in skills)
                dict.Add(skill.id, skill);
            return dict;
        }
    }
    #endregion

    #region Item
    [Serializable]
    public class ItemData
    {
        public int id;
        public string name;
        public ItemType itemType;
        public string iconPath;
    }

    public class ConsumableData : ItemData
    {
        public ConsumableType consumableType;
        public int maxCount;
    }

    [Serializable]
    public class ItemLoader : ILoader<int, ItemData>
    {
        public List<ConsumableData> consumables = new List<ConsumableData>();

        public Dictionary<int, ItemData> MakeDict()
        {
            Dictionary<int, ItemData> dict = new Dictionary<int, ItemData>();
            foreach (ItemData item in consumables)
            {
                item.itemType = ItemType.Consumable;
                dict.Add(item.id, item);
            }
            return dict;
        }
    }
    #endregion

    #region Monster

    [Serializable]
    public class RewardData
    {
        public int probability; // 100분율
        public int itemId;
        public int count;
    }

    [Serializable]
    public class PokemonLoader : ILoader<int, PokemonData>
    {
        public List<PokemonData> pokemons = new List<PokemonData>();

        public Dictionary<int, PokemonData> MakeDict()
        {
            Dictionary<int, PokemonData> dict = new Dictionary<int, PokemonData>();
            foreach (PokemonData pokemon in pokemons)
            {
                dict.Add(pokemon.Id, pokemon);
            }
            return dict;
        }
    }

    #endregion
}
