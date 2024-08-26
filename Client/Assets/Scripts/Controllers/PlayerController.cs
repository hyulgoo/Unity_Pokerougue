using Data;
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

    private void Awake()
    {
        //DontDestroyOnLoad(this);
    }

    public void AddPokemon(int pokemonNumber)
    {
        PokemonData data = Managers.Data.MonsterDict[pokemonNumber];
        _pokemonDatas.Add(data);
    }

    public void ReleasePokemon(int order) { _pokemonDatas.RemoveAt(order); }

    public PokemonInfo GetCurMonsterInfo() { return GetMonsterInfo(0); }
    public PokemonInfo GetMonsterInfo(int order) { return GetMonsterData(order).info; }

    public string GetCurMonsterName() { return GetMonsterName(0); }
    public string GetMonsterName(int order) { return _pokemonDatas[order].name; }    

    public PokemonData GetCurMonsterData() { return _pokemonDatas[0]; }
    public PokemonData GetMonsterData(int order) { return _pokemonDatas[order]; }

    public PokemonInfo[] GetAllMonsterinfo()
    {
        PokemonData[] datas = GetAllMonsterData();
        int count = datas.Count();

        PokemonInfo[] infos = new PokemonInfo[count];
        for(int i = 0; i < count; ++i)
            infos[i] = datas[i].info;

        return infos;
    }

    public PokemonData[] GetAllMonsterData() { return _pokemonDatas.ToArray(); }

    public void ChangeMonsterOrder(int changetoCurorder)
    {
        PokemonData tmp = _pokemonDatas[changetoCurorder];
        _pokemonDatas.RemoveAt(changetoCurorder);
        _pokemonDatas.Insert(0, tmp);
    }

    public void SetMonsterData(PokemonList list)
    {
        for (int i = 0; i < list.Pokemon.Count(); ++i)
            _pokemonDatas.Add(Managers.Data.MonsterDict[list.Pokemon[i]]);
    }
}
