using Data;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public string Name { get; set; }
    public int Id { get; set; }

    List<PokemonData> _pokemonDatas = new List<PokemonData>();

    public Dictionary<int, Dictionary<int, int>> SkillPP { get; } = new Dictionary<int, Dictionary<int, int>>();

    private void Awake()
    {
        //DontDestroyOnLoad(this);
    }

    public void AddPokemon(int pokemonNumber)
    {
        PokemonData data = Managers.Data.PokeonDict[pokemonNumber];
        _pokemonDatas.Add(data);
    }

    public void ReleasePokemon(int order) { _pokemonDatas.RemoveAt(order); }

    public PokemonInfo GetCurMonsterInfo() { return GetMonsterInfo(0); }
    public PokemonInfo GetMonsterInfo(int order) { return GetMonsterData(order).Info; }

    public string GetCurPokemonName() { return GetMonsterName(0); }
    public string GetMonsterName(int order) { return _pokemonDatas[order].Name; }    

    public PokemonData GetCurPokemonData() { return _pokemonDatas[0]; }
    public PokemonData GetMonsterData(int order) { return _pokemonDatas[order]; }

    public PokemonInfo[] GetAllMonsterinfo()
    {
        PokemonData[] datas = GetAllMonsterData();
        int count = datas.Count();

        PokemonInfo[] infos = new PokemonInfo[count];
        for(int i = 0; i < count; ++i)
            infos[i] = datas[i].Info;

        return infos;
    }

    public PokemonData[] GetAllMonsterData() { return _pokemonDatas.ToArray(); }

    public void ChangeMonsterOrder(int changetoCurorder)
    {
        PokemonData tmp = _pokemonDatas[changetoCurorder];
        _pokemonDatas.RemoveAt(changetoCurorder);
        _pokemonDatas.Insert(0, tmp);
    }

    public void SetMonsterData(RepeatedField<int> list)
    {
        for (int i = 0; i < list.Count(); ++i)
        { 
            // 포켓몬 정보 추가
            _pokemonDatas.Add(Managers.Data.PokeonDict[list[i]]);

            // 스킬 pp 정보 추가
            RepeatedField<int> skillIdList = Managers.Data.PokeonDict[list[i]].Info.SkillId;
            Dictionary<int, int> skillppDict = new Dictionary<int, int>();
            for(int j = 0; j < skillIdList.Count(); ++j)
            {
                skillppDict.Add(skillIdList[j], Managers.Data.SkillDict[skillIdList[j]].info.Pp);
            }
            SkillPP.Add(list[i], skillppDict);
        }
    }
}
