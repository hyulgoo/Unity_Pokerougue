using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;

namespace Server.Data
{
    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        public static Dictionary<int, SkillData> SkillDict { get; private set; } = new Dictionary<int, SkillData>();
        public static Dictionary<int, ItemData> ItemDict { get; private set; } = new Dictionary<int, ItemData>();

        public static Dictionary<int, PokemonData> PokemonDict { get; private set; } =
            new Dictionary<int, PokemonData>();

        public static void LoadData()
        {
            SkillDict = LoadJson<SkillLoader, int, SkillData>("SkillData").MakeDict();
            ItemDict = LoadJson<ItemLoader, int, ItemData>("ItemData").MakeDict();
            PokemonDict = LoadJson<PokemonLoader, int, PokemonData>("PokemonData").MakeDict();
        }

        private static Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{path}.json");
            return JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}