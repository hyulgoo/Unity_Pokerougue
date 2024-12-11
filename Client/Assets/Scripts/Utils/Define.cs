using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public const int PokemonMaxCount = 6;
    public const int InValidNumber = -1;

    public enum Scene
    {
        Unknown,
        Login,
        Lobby,
        Game,
        Select,
        Battle,
    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }

    public enum UIEvent
    {
        Click,
        Drag,
    }    

    
}
