using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UIBattleScene : UICommonScene
{
    private string _announceText = string.Empty;

    private int[] _HpbarSpriteNum = new int[(int)TargetType.End] { Define.InValidNumber, Define.InValidNumber };
    private Sprite[] _hpbarSprites;
    public Sprite[] HpSprites { get { return _hpbarSprites; } }

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

        GetObject((int)GameObjects.MyHpbar).GetComponent<Slider>().value = 0f;
        GetObject((int)GameObjects.EnemyHpbar).GetComponent<Slider>().value = 0f;

        SetField(Managers.Player.ArenaType);
        SetPlayerInfo();
    }

    public void SetPlayerInfo()
    {
        _hpbarSprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/overlay_hp");

        SetBattlePokemonInfo();
        MyTurn();
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
        int _myPokemonId = Managers.Player.MyPlayer.GetCurPokemonData().Id;
        int _enemyPokemonId = Managers.Player.Enemy.GetCurPokemonData().Id;

        // 내 포켓몬 이름 설정
        GetText((int)Texts.Text_MyName).text = Managers.Data.PokeonDict[_myPokemonId].Name;
        Sprite myImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/back/{_myPokemonId}")[0]; ;
        GetImage((int)Images.Image_My).sprite = myImage;

        // 상대방 포켓몬 이름 설정
        GetText((int)Texts.Text_EnemyName).text = Managers.Data.PokeonDict[_enemyPokemonId].Name;
        Sprite enemyImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{_enemyPokemonId}")[0];
        GetImage((int)Images.Image_Enemy).sprite = enemyImage;

        SetHPBar(targetRatio: 1f, targetType: TargetType.Oneself, false);
        SetHPBar(targetRatio: 1f, targetType: TargetType.Enemy, false);
        SetAnnounce(string.Empty, true);
    }

    public void MyTurn()
    {
        if (Managers.Player.MyTurn)
        {
            Managers.UI.ShowPopupUI<UIBattleBehaviorSelectPopup>();
            string myPokemonName = Managers.Data.PokeonDict[Managers.Player.MyPlayer.GetCurPokemonData().Id].Name;
            Managers.Job.Push(() => SetAnnounce($"{myPokemonName}(은)는 무엇을 할까?", true) );
        }
        else
        {
            Managers.Job.Push(() => SetAnnounce("상대 차례를 기다리는 중", true));
        }
    }

    public void CurrentPokemonFallDown()
    {
        Managers.UI.ShowPopupUI<UIBattlePokemonChangePopup>();
    }

    public void SetAnnounce(string announce, bool setByHandler)
    {
        _announceText = announce;
        StartCoroutine(SetTextCoroutine(setByHandler));
    }

    IEnumerator SetTextCoroutine(bool setByHandler)
    {
        GetText((int)Texts.Announce).text = "";
        foreach (char value in _announceText)
        {
            GetText((int)Texts.Announce).text += value;
            yield return new WaitForSeconds(Managers.UI.UISpeed);
        }

        if (setByHandler)
            Managers.Job.Excute();
    }

    const float targetDiff = 0.05f;
    const float magnification = 1.5f;

    public void SetHPBar(float targetRatio, TargetType targetType, bool setByHandler = true)
    {
        StartCoroutine(SetHPbarCoroutine(targetRatio, targetType, setByHandler));
    }

    IEnumerator SetHPbarCoroutine(float targetRatio, TargetType targetType, bool setByHandler)
    {
        Slider slider = GetObject((int)targetType).GetComponent<Slider>();
        GameObject fillObj = Util.FindChild(GetObject((int)targetType), "Fill", true);
        float curRatio = slider.value;

        while (Mathf.Abs(targetRatio - curRatio) > Mathf.Abs(targetDiff))
        {
            int hpstate = 2 - (int)(curRatio / 0.34f);
            if (_HpbarSpriteNum[(int)targetType] != hpstate)
            {
                _HpbarSpriteNum[(int)targetType] = hpstate;
                fillObj.GetComponent<Image>().sprite = _hpbarSprites[hpstate];
            }

            curRatio = Mathf.Lerp(slider.value, targetRatio, Time.deltaTime * magnification);
            slider.value = curRatio;

            yield return null;
        }

        slider.value = targetRatio;
        if(setByHandler)
            Managers.Job.Excute();
    }
}
