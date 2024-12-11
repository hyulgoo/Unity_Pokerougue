using Data;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CommonPlayerController : MonoBehaviour
{
    public string Name { get; set; }
    public int Id { get; set; }

    List<PokemonData> _pokemonDataList = new List<PokemonData>();

    public Dictionary<int, Dictionary<int, int>> SkillPP { get; } = new Dictionary<int, Dictionary<int, int>>();

    private void Awake()
    {
        //DontDestroyOnLoad(this);
    }

    public void AddPokemon(int pokemonNumber)
    {
        PokemonData data = Managers.Data.PokeonDict[pokemonNumber];
        _pokemonDataList.Add(data);
    }

    public void ReleasePokemon(int order) { _pokemonDataList.RemoveAt(order); }

    public PokemonInfo GetCurPokemonInfo() { return GetPokemonInfo(0); }
    public PokemonInfo GetPokemonInfo(int order) { return GetPokemonData(order).Info; }

    public string GetCurPokemonName() { return GetPokemonName(0); }
    public string GetPokemonName(int order) { return _pokemonDataList[order].Name; }    

    public PokemonData GetCurPokemonData() { return _pokemonDataList[0]; }
    public PokemonData GetPokemonData(int order) { return _pokemonDataList[order]; }

    public PokemonInfo[] GetAllPokemoninfo()
    {
        PokemonData[] datas = GetAllPokemonData();
        int count = datas.Count();

        PokemonInfo[] infos = new PokemonInfo[count];
        for(int i = 0; i < count; ++i)
            infos[i] = datas[i].Info;

        return infos;
    }

    public PokemonData[] GetAllPokemonData() { return _pokemonDataList.ToArray(); }

    public void SetPokemonData(RepeatedField<int> list)
    {
        for (int i = 0; i < list.Count(); ++i)
        { 
            // 포켓몬 정보 추가
            _pokemonDataList.Add(Managers.Data.PokeonDict[list[i]]);

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

    public void ChangePokemonOrderById(int id)
    {
        for (int index = 0; index < _pokemonDataList.Count; ++index)
        {
            if (_pokemonDataList[index].Id != id)
                continue;

            (_pokemonDataList[0], _pokemonDataList[index]) = (_pokemonDataList[index], _pokemonDataList[0]);
            return;
        }

        Debug.Assert(false, "Cannot Found ChangePokemon!!");
    }
}
