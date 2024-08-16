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
          
    #region CalcAttackType
    public float CalcAttackType(Type type, Type opponent)
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
                else if(opponent == Type.Steel)
                    damageRatio = 0.5f;
                else if( opponent == Type.Fairy)
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
}
