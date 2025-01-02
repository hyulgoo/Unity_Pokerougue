using Google.Protobuf.Protocol;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBattleScene : UICommonScene
{
    private string _announceText = string.Empty;

    private int[] _HpbarSpriteNum = new int[(int)TargetType.End] { Define.InValidNumber, Define.InValidNumber };
    private Sprite[] _hpbarSprites;
    public Sprite[] HpSprites { get { return _hpbarSprites; } }
    private Sprite[] _numberSprites;
    float _screenWidth;

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

        Image_BackGround,
        Image_Enemy_FootHold,
        Image_My_FootHold,

        Image_MyPokemon,
        Image_MyTrainer,
        Image_MyPokeball,
        Image_EnemyPokemon,
        Image_EnemyTrainer,
        Image_EnemyPokeball,
    }

    enum Texts
    {
        Text_MyPokemonName,
        Text_EnemyPokemonName,
        Text_Announce
    }

    enum Animators
    {
        Image_MyPokemon,
        Image_EnemyPokemon
    }

    public override void Init()
    {
        base.Init();

        _screenWidth = (float)(Screen.width);
        _hpbarSprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/overlay_hp");
        _numberSprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/numbers");

        BindObject(typeof(GameObjects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        Bind<Animator>(typeof(Animators));

        GetObject((int)GameObjects.MyHpbar).GetComponent<Slider>().value = 0f;
        GetObject((int)GameObjects.EnemyHpbar).GetComponent<Slider>().value = 0f;

        SetField(Managers.Player.ArenaType);
        SetPlayerInfo();
    }

    public void SetPlayerInfo()
    {
        GetText((int)Texts.Text_MyPokemonName).text = string.Empty;
        Sprite myTrainerImage = Managers.Resource.Load<Sprite>($"Sprite/character/trainer_f_back"); ;
        SetTrainerOrPokemonImage(myTrainerImage, true, true);

        GetText((int)Texts.Text_EnemyPokemonName).text = string.Empty;
        Sprite enemyTrainerImage = Managers.Resource.Load<Sprite>($"Sprite/character/rival_f 1");
        SetTrainerOrPokemonImage(enemyTrainerImage, false, true);

        string enemyName = Managers.Player.Enemy.Name;
        Managers.Job.Push(() => { SetAnnounce($"{enemyName}가 승부를 걸어왔다!", true); });

        string currentPokemonName = Managers.Player.MyPlayer.GetCurPokemonName();
        Managers.Job.Push(() =>
        {
            MoveTrainerAtSpawnPokemon();
            SetAnnounce($"가라! {currentPokemonName}!", true);
        });

        Managers.Job.Push(() => { SetBattlePokemonInfo(true); SetBattlePokemonInfo(false); });

        NewTurn();
    }

    public void SetField(int type)
    {
        Arenas arenatype = (Arenas)type;

        const string path = "Sprite/arenas/";
        string arenaName = path + arenatype.ToString();

        string fieldName = arenaName + "_bg";
        GetImage((int)Images.Image_BackGround).sprite = Managers.Resource.Load<Sprite>(fieldName);
        string myfoothold = arenaName + "_a";
        GetImage((int)Images.Image_My_FootHold).sprite = Managers.Resource.Load<Sprite>(myfoothold);
        string enemyfoothold = arenaName + "_b";
        GetImage((int)Images.Image_Enemy_FootHold).sprite = Managers.Resource.Load<Sprite>(enemyfoothold);

        if (GetImage((int)Images.Image_BackGround).sprite == null)
            Debug.Log($"Fail to Find Background Sprite ({fieldName})");

        FootHoldMoveAtStart();
    }

    const float FootHoldMoveTime = 1.5f;

    void FootHoldMoveAtStart()
    {
        StartCoroutine(ImageHorizonMoveCoroutine(Images.Image_My_FootHold, Images.Image_Enemy_FootHold, FootHoldMoveTime, _screenWidth, true, true));
    }

    IEnumerator ImageHorizonMoveCoroutine(Images myImage, Images enemyImage, float moveTime, float moveDistance, bool isReturn, bool setByHandler)
    {
        Transform myTransform = GetImage((int)myImage).transform;
        Transform enemyTransform = GetImage((int)enemyImage).transform;

        if (myTransform == null || enemyTransform == null)
        {
            Debug.Assert(false, "ImageTransform이 null입니다.");
        }

        float accumulatedTime = 0f;

        Vector3 myTransformOriginPosition = myTransform.position;
        Vector3 enemyTransformOriginPosition = enemyTransform.position;

        if (isReturn)
        {
            myTransform.position = myTransformOriginPosition + new Vector3(moveDistance, 0f, 0f);
            enemyTransform.position = enemyTransformOriginPosition - new Vector3(moveDistance, 0f, 0f);
        }

        while (accumulatedTime < moveTime)
        {
            accumulatedTime += Time.deltaTime;
            float diffX = moveDistance * (Time.deltaTime / moveTime);

            Vector3 newPosition = myTransform.position;
            newPosition.x =  newPosition.x - diffX ;
            myTransform.position = newPosition;

            newPosition = enemyTransform.position;
            newPosition.x = newPosition.x + diffX;
            enemyTransform.position = newPosition;

            yield return null;
        }

        myTransform.position = myTransformOriginPosition;
        enemyTransform.position = enemyTransformOriginPosition;

        if(setByHandler)
            Managers.Job.Excute();
    }

    IEnumerator ThrowPokeballCroutine(float spawnTime, bool isMe, bool setByHandler)
    {
        float accumulatedTime = 0f;
        accumulatedTime += Time.deltaTime;

        while (accumulatedTime < spawnTime)
        {
            Image targetImage = isMe ? GetImage((int)Images.Image_MyPokeball) : GetImage((int)Images.Image_EnemyPokeball);
            targetImage.color = Color.white;

            Animator targetAnimator = targetImage.GetComponent<Animator>();

            if (targetAnimator == null)
            {
                Debug.Assert(false, "Pokeball Animator를 찾을 수 없습니다.");
            }

            string statName = isMe ? "ThrowPokeball" : "SpawnPokemon_0";
            targetAnimator.Play(statName);

            yield return null;
        }

        SetBattlePokemonInfo(isMe);

        if (setByHandler)
            Managers.Job.Excute();
    }

    const float TrainerMoveTimeAtSpawnPokemon = 1f;
    const float ThrowPokeballTime = 0.7f;

    void MoveTrainerAtSpawnPokemon()
    {
        StartCoroutine(ImageHorizonMoveCoroutine(Images.Image_MyTrainer, Images.Image_EnemyTrainer, TrainerMoveTimeAtSpawnPokemon, _screenWidth * 0.7f, false, false));
    }

    public void ThrowPokeball(bool isMe, bool setByHandler)
    {
        StartCoroutine(ThrowPokeballCroutine(ThrowPokeballTime, isMe, setByHandler));
    }

    public void SetBattlePokemonInfo(bool isMe)
    {
        // MyInfo
        if(isMe)
        {
            ThrowPokeball(isMe, false);
            PokemonData myPokemonData = Managers.Player.MyPlayer.GetCurPokemonData();
            GetText((int)Texts.Text_MyPokemonName).text = Managers.Data.PokeonDict[myPokemonData.Id].Name;
            Sprite myPokemonImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/back/{myPokemonData.Id}")[0];
            SetTrainerOrPokemonImage(myPokemonImage, true, false);

            SetHPBar(targetRatio: 1f, targetType: TargetType.Oneself, false);
            SetPokemonLevelInfo(true, myPokemonData.Info.Level);
        }

        // EnemyInfo
        else
        {
            ThrowPokeball(isMe, false);
            PokemonData enemyPokemonData = Managers.Player.Enemy.GetCurPokemonData();
            GetText((int)Texts.Text_EnemyPokemonName).text = Managers.Data.PokeonDict[enemyPokemonData.Id].Name;
            Sprite enemyPokemonImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{enemyPokemonData.Id}")[0];
            SetTrainerOrPokemonImage(enemyPokemonImage, false, false);

            SetHPBar(targetRatio: 1f, targetType: TargetType.Enemy, false);
            SetPokemonLevelInfo(false, enemyPokemonData.Info.Level);
        }

        Managers.Job.Excute();
    }

    void SetPokemonLevelInfo(bool isMe, int level)
    {
        Image level000 = isMe ? GetImage((int)Images.Image_MyLevel_000) : GetImage((int)Images.Image_EnemyLevel_000);
        Image level00 = isMe ? GetImage((int)Images.Image_MyLevel_00) : GetImage((int)Images.Image_EnemyLevel_00);
        Image level0 = isMe ? GetImage((int)Images.Image_MyLevel_0) : GetImage((int)Images.Image_EnemyLevel_0);

        level000.sprite = level == 100 ? _numberSprites[1] : null;
        level000.color = level == 100 ? Color.white : Color.clear;

        int number00 = (level % 100) / 10;
        level00.sprite = _numberSprites[number00];
        level00.color = level >= 10 ? Color.white : Color.clear;

        int number0 = level % 10;
        level0.sprite = _numberSprites[number0];
        level0.color = Color.white;
    }

    public void SetTrainerOrPokemonImage(Sprite sprite, bool isMe, bool isTrainer)
    {
        Image targetImage = isMe ? (isTrainer ? GetImage((int)Images.Image_MyTrainer) : GetImage((int)Images.Image_MyPokemon)) 
                                    : (isTrainer ? GetImage((int)Images.Image_EnemyTrainer) : GetImage((int)Images.Image_EnemyPokemon));
        Image relativeImage = isMe ? (isTrainer ? GetImage((int)Images.Image_MyPokemon) : GetImage((int)Images.Image_MyTrainer)) 
                                    : (isTrainer ? GetImage((int)Images.Image_EnemyPokemon) : GetImage((int)Images.Image_EnemyTrainer));

        targetImage.sprite = sprite;
        targetImage.color = Color.white;
        relativeImage.color = Color.clear;
        if(isTrainer == false)
            StartCoroutine(SetSpawnPokemonScale(targetImage, isMe));
    }

    IEnumerator SetSpawnPokemonScale(Image image, bool isMe)
    {
        Transform imageTransform = image.transform;
        float accumulatedTime = 0f;

        //int curPokemonId = isMe ? Managers.Player.MyPlayer.GetCurPokemonData().Id : Managers.Player.Enemy.GetCurPokemonData().Id;
        //string stateName = isMe ? curPokemonId.ToString() + "_back" : curPokemonId.ToString();
        //Animator curAnimator = Get<Animator>(isMe ? (int)Animators.Image_MyPokemon : (int)Animators.Image_EnemyPokemon);
        //curAnimator.Play(stateName);

        while (accumulatedTime < 1f)
        {
            accumulatedTime += Time.deltaTime * 3;
            imageTransform.localScale = Vector3.one * accumulatedTime;

            yield return null;
        }

        imageTransform.localScale = Vector3.one;
    }

    public void NewTurn()
    {
        if (Managers.Player.MyTurn)
        {
            Managers.UI.ShowPopupUI<UIBattleBehaviorSelectPopup>();
            string myPokemonName = Managers.Data.PokeonDict[Managers.Player.MyPlayer.GetCurPokemonData().Id].Name;
            Managers.Job.Push(() => SetAnnounce($"{myPokemonName}(은)는 무엇을 할까?", true));
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
        string text = isWin ? (isRunaway ? $"{Managers.Player.Enemy.Name}(은)는 도망쳤다!" : $"{Managers.Player.Enemy.Name}(와)과의 대결에서 승리했다!") 
            : (isRunaway ? "무사히 도망쳤다!" :$"{Managers.Player.MyPlayer.Name}(은)는 눈 앞이 캄캄해졌다.");
        Managers.Job.Push(() => { SetAnnounce(text, true); });
        Managers.Player.IsTurnProgressing = false;
    }

    public void SetAnnounce(string announce, bool setByHandler)
    {
        _announceText = announce;
        StartCoroutine(SetTextCoroutine(setByHandler));
    }

    IEnumerator SetTextCoroutine(bool setByHandler)
    {
        TMP_Text announceText = GetText((int)Texts.Text_Announce);

        announceText.text = string.Empty;
        foreach (char value in _announceText)
        {
            announceText.text += value;
            yield return new WaitForSeconds(Managers.UI.UISpeed);
        }

        if (setByHandler)
            Managers.Job.Excute(); 
    }

    const float targetHPSliderDiff = 0.02f;
    const float hpSliderSpeedMagnification = 1.5f;

    public void SetHPBar(float targetRatio, TargetType targetType, bool setByHandler = true)
    {
        StartCoroutine(SetHPbarCoroutine(targetRatio, targetType, setByHandler));
    }

    IEnumerator SetHPbarCoroutine(float targetRatio, TargetType targetType, bool setByHandler)
    {
        Slider slider = GetObject((int)targetType).GetComponent<Slider>();
        GameObject fillObj = Util.FindChild(GetObject((int)targetType), "Fill", true);
        float curRatio = slider.value;

        while (Mathf.Abs(targetRatio - curRatio) > Mathf.Abs(targetHPSliderDiff))
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

            curRatio = Mathf.Lerp(curRatio, targetRatio, Time.deltaTime * hpSliderSpeedMagnification);
            slider.value = curRatio;

            yield return null;
        }

        slider.value = targetRatio;

        if(setByHandler)
            Managers.Job.Excute();
    }
}
