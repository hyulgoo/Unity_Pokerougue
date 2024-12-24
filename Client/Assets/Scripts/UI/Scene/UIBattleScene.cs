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
    }

    enum Images
    {
        Image_EnemyLevel_0,
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
        string currentPokemonName = Managers.Player.MyPlayer.GetCurPokemonName();
        Managers.Job.Push(()=> { SetAnnounce($"가라! {currentPokemonName}!", true); });
        Managers.Job.Excute();
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
            Debug.Log($"Fail to Find Background Sprite ({fieldName})");
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

    public void ChangeFalldownPokemon()
    {
        UIBattlePokemonChangePopup battlePokemonChangePopup = Managers.UI.ShowPopupUI<UIBattlePokemonChangePopup>();
        battlePokemonChangePopup.BattleScene = this;
        battlePokemonChangePopup.MustChange = true;
    }

    public void DuelEnd(bool isWin, bool isRunaway)
    {
        string text = isWin ? (isRunaway ? $"{Managers.Player.Enemy.Name}(은)는 도망쳤다!" : $"{Managers.Player.Enemy.Name}(와)과의 대결에서 승리했다!") : (isRunaway ? "무사히 도망쳤다!" :$"{Managers.Player.MyPlayer.Name}(은)는 눈 앞이 캄캄해졌다.");
        Managers.Job.Push(() => SetAnnounce(text, true));
    }

    public void SetAnnounce(string announce, bool setByHandler)
    {
        _announceText = announce;
        StartCoroutine(SetTextCoroutine(setByHandler));
    }

    IEnumerator SetTextCoroutine(bool setByHandler)
    {
        GetText((int)Texts.Announce).text = string.Empty;
        foreach (char value in _announceText)
        {
            GetText((int)Texts.Announce).text += value;
            yield return new WaitForSeconds(Managers.UI.UISpeed);
        }

        if (setByHandler)
            Managers.Job.Excute(); 
    }

    const float targetDiff = 0.02f;
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
            int hpState = 2 - (int)(curRatio / 0.34f);
            if (hpState > 2)
                hpState = 2;
            else if (hpState < 0)
                hpState = 0;

            if (_HpbarSpriteNum[(int)targetType] != hpState)
            {
                _HpbarSpriteNum[(int)targetType] = hpState;
                fillObj.GetComponent<Image>().sprite = _hpbarSprites[hpState];
            }

            curRatio = Mathf.Lerp(curRatio, targetRatio, Time.deltaTime * magnification);
            slider.value = curRatio;

            yield return null;
        }

        slider.value = targetRatio;
        if(setByHandler)
            Managers.Job.Excute();
    }
}
