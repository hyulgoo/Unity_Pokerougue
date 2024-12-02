using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UI_BattleScene : UI_Scene
{
    int _myPokemonId;
    int _enemyPokemonId;
    string _announceText = "";

    Sprite[] _hpbarSprites;
    int[] _HpbarSpriteNum = new int[(int)TargetType.End] { -1, -1 };

    enum GameObjects
    {
        MyHpbar,
        EnemyHpbar,
        End
    }

    enum Images
    {
        Image_EnemyLevel_,
        Image_EnemyLevel_00,
        Image_EnemyLevel_000,

        Image_MyLevel_0,
        Image_MyLevel_00,
        Image_MyLevel_000,

        BackGround,
        Enemy_FootHold,
        My_FootHold,

        Image_My,
        Image_Enemy,
    }

    enum Texts
    {
        Text_MyName,
        Text_EnemyName,
        Announce
    }

    public override void Init()
    {
        base.Init();

        BindObject(typeof(GameObjects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));

        SetField(Managers.Object.ArenaType);
        SetPlayerInfo();
    }

    public void SetPlayerInfo()
    {
        _hpbarSprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/overlay_hp");
        _myPokemonId = Managers.Object.MyPlayer.GetCurPokemonData().Id;
        _enemyPokemonId = Managers.Object.Enemy.GetCurPokemonData().Id;

        SetBattlePokemonInfo();
    }

    public void SetField(int type)
    {
        Arenas arenatype = (Arenas)type;
        // 필드를 불러옴
        const string path = "Sprite/arenas/";
        string arenaName = path + arenatype.ToString();

        string fieldName = arenaName + "_bg";
        GetImage((int)Images.BackGround).sprite = Managers.Resource.Load<Sprite>(fieldName);
        string myfoothold = arenaName + "_a";
        GetImage((int)Images.My_FootHold).sprite = Managers.Resource.Load<Sprite>(myfoothold);
        string enemyfoothold = arenaName + "_b";
        GetImage((int)Images.Enemy_FootHold).sprite = Managers.Resource.Load<Sprite>(enemyfoothold);

        if (GetImage((int)Images.BackGround).sprite == null)
            Debug.Log($"{fieldName}");
    }

    public void SetBattlePokemonInfo()
    {
        // 내 포켓몬 이름 설정
        GetText((int)Texts.Text_MyName).text = Managers.Data.PokeonDict[_myPokemonId].Name;
        Sprite myImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/back/{_myPokemonId}")[0]; ;
        GetImage((int)Images.Image_My).sprite = myImage;

        // 상대방 포켓몬 이름 설정
        GetText((int)Texts.Text_EnemyName).text = Managers.Data.PokeonDict[_enemyPokemonId].Name;
        Sprite enemyImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{_enemyPokemonId}")[0];
        GetImage((int)Images.Image_Enemy).sprite = enemyImage;

        SetHPBar(targetRatio: 1f, targetType: TargetType.Oneself);
        SetHPBar(targetRatio: 1f, targetType: TargetType.Enemy);

        MyTurn();
    }

    public void MyTurn()
    {
        if (Managers.Object.MyTurn)
        {
            Managers.UI.ShowPopupUI<UI_SelectBehaviorPopup>();
            SetAnnounce($"{Managers.Data.PokeonDict[_myPokemonId].Name}는 무엇을 할까?");
        }
        else
        {
            SetAnnounce("상대 차례를 기다리는 중");
        }
    }

    public void SetAnnounce(string announce, bool setByHandler = true)
    {
        _announceText = announce;
        StartCoroutine(SetTextCoroutine(setByHandler));
    }

    #region HP

    const float targetDiff = 0.01f;
    const float magnification = 10f;

    public void SetHPBar(float targetRatio, TargetType targetType)
    {
        StartCoroutine(SetHPbarCoroutine(targetRatio, targetType));
    }

    IEnumerator SetHPbarCoroutine(float targetRatio, TargetType targetType)
    {
        Slider slider = GetObject((int)targetType).GetComponent<Slider>();

        GameObject fillObj = Util.FindChild(GetObject((int)targetType), "Fill", true);
        Debug.Log("Cannot Found Fill Object");

        float curRatio = slider.value;

        while (targetRatio - curRatio < targetDiff)
        {
            int hpstate = 2 - (int)(curRatio / 0.34f);
            if (_HpbarSpriteNum[(int)targetType] != hpstate)
            {
                _HpbarSpriteNum[(int)targetType] = hpstate;
                fillObj.GetComponent<Image>().sprite = _hpbarSprites[hpstate];
            }

            curRatio = slider.value + (Managers.UI.UISpeed * magnification * Time.deltaTime);
            slider.value = curRatio;

            yield return null;
        }
    }

    #endregion

    IEnumerator SetTextCoroutine(bool setbyHandler)
    {
        GetText((int)Texts.Announce).text = "";
        for(int i = 0; i < _announceText.Length; ++i)
        {
            GetText((int)Texts.Announce).text += _announceText[i];
            yield return new WaitForSeconds(Managers.UI.UISpeed);
        }

        if (setbyHandler)
            Managers.Job.Excute();
    }
}
