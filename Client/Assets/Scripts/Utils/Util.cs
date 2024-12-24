using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
		if (component == null)
            component = go.AddComponent<T>();
        return component;
	}

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        if (transform == null)
            return null;
        
        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
		}
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }
              
    public static string GetTypeName(Type type)
    {
        string typeName = string.Empty;
        switch (type)
        {
            case Type.Notype:
                typeName = "없음";
                break;
            case Type.Bug:
                typeName = "벌레";
                break;
            case Type.Dark:
                typeName = "악";
                break;
            case Type.Dragon:
                typeName = "드래곤";
                break;
            case Type.Electric:
                typeName = "전기";
                break;
            case Type.Fairy:
                typeName = "페어리";
                break;
            case Type.Fighting:
                typeName = "격투";
                break;
            case Type.Fire:
                typeName = "불꽃";
                break;
            case Type.Flying:
                typeName = "비행";
                break;
            case Type.Ghost:
                typeName = "고스트";
                break;
            case Type.Grass:
                typeName = "풀";
                break;
            case Type.Ground:
                typeName = "땅";
                break;
            case Type.Ice:
                typeName = "얼음";
                break;
            case Type.Normal:
                typeName = "노말";
                break;
            case Type.Poison:
                typeName = "독";
                break;
            case Type.Psychic:
                typeName = "에스퍼";
                break;
            case Type.Rock:
                typeName = "바위";
                break;
            case Type.Steel:
                typeName = "강철";
                break;
            case Type.Water:
                typeName = "물";
                break;
        }

        return typeName;
    }

    public static float GetPokemonHPRatio(PokemonData data)
    {
        int maxHp = Managers.Data.PokeonDict[data.Id].Info.Hp;
        int curHp = data.Info.Hp;
        float ratio = (float)curHp / (float)maxHp;

        if (ratio > 1f)
            ratio = 1f;
        else if (ratio < 0f)
            ratio = 0f;
                
        return ratio;
    }
}
