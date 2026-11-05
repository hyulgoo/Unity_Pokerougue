using Google.Protobuf.Protocol;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBattleScene : UICommonScene
{
    float _screenWidth;

    private string _announceText = string.Empty;

    // HPBar
    private int[] _HpbarSpriteNum = new int[(int)TargetType.End] { Define.InValidNumber, Define.InValidNumber };
    private Sprite[] _hpbarSprites;
    public Sprite[] HpSprites { get { return _hpbarSprites; } }
    private Sprite[] _numberSprites;

    // init Anim
    const float FootHoldMoveTime = 1.5f;

    const float TrainerMoveTimeAtSpawnPokemon = 1f;

    const float targetHPSliderDiff = 0.02f;
    const float hpSliderSpeedMagnification = 1.5f;

    // 아래 연출 수치는 원작(PokeRogue)의 값이다. 좌표는 원작 화면(320x180) 기준이라 비율로 바꿔 쓴다.
    const float OriginalScreenWidth = 320f;
    const float OriginalScreenHeight = 180f;

    // 소환 (summon-phase.ts, switch-summon-phase.ts)
    const float PlayerThrowDelay = 0.75f;
    const float EnemyThrowDelay = 0.35f;
    const float TrainerMoveDistanceRatio = 0.4f;
    const float BallFlightTime = 0.65f;
    const float BallRiseTime = 0.15f;
    const float BallFallTime = 0.5f;
    const float BallSpinAngle = 1440f;
    const float SummonScaleTime = 0.25f;
    const float SummonStartScale = 0.5f;
    const float RecallToSummonDelay = 0.75f;
    static readonly Color BallTintColor = new Color32(0xd5, 0x29, 0x29, 0xff);

    // 몬스터볼이 열리는 프레임 (원작은 파티클이라 기존 스프라이트 연출의 시간을 유지한다)
    const float BallOpenFrameTime = 1f / 12f;
    const float BallBurstScale = 5f;
    static readonly float[] BallBurstFrameTimes = { 1f / 6f, 0.25f, 1f / 12f };

    // 피격 (damage-anim-phase.ts: 0.1초 간격으로 꺼짐/켜짐을 세 번 반복한다)
    const float HitFlashInterval = 0.1f;
    const int HitFlashToggleCount = 5;

    // 기절 (faint-phase.ts)
    const float FaintTime = 0.5f;
    const float FaintCryPitch = 0.85f;
    const float FaintCryMaxTime = 3f;

    static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    Sprite _ballSprite;
    Sprite _ballOpeningSprite;
    Sprite _ballOpenSprite;
    Sprite[] _ballBurstSprites;
    Material _myPokemonMaterial;
    Material _enemyPokemonMaterial;
    BattleAnimPlayer _battleAnimPlayer;

    enum GameObjects
    {
        MyHpbar,
        EnemyHpbar,
    }

    public enum Images
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
        Image_MyPokemon_Effect,
        Image_MyTrainer,
        Image_MyPokeball,
        Image_EnemyPokemon,
        Image_EnemyPokemon_Effect,
        Image_EnemyTrainer,
        Image_EnemyPokeball,
    }

    enum Texts
    {
        Text_MyPokemonName,
        Text_EnemyPokemonName,
        Text_Announce
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

        LoadSummonResources();
        _battleAnimPlayer = new BattleAnimPlayer(
            GetImage((int)Images.Image_MyPokemon),
            GetImage((int)Images.Image_EnemyPokemon),
            GetImage((int)Images.Image_BackGround).rectTransform,
            transform);

        // Field
        {
            SetField(Managers.Player.ArenaType);

            string enemyName = Managers.Player.Enemy.Name;
            Managers.Job.Push(() => { SetAnnounce($"{enemyName}가 승부를 걸어왔다!", true); });

            FootHoldMoveAtDuelStart();
        }

        // Player
        {
            SetPlayerInfo();

            string currentPokemonName = Managers.Player.MyPlayer.GetCurPokemonName();
            Managers.Job.Push(() => { SetAnnounce($"가라! {currentPokemonName}!", true); });
            Managers.Job.Push(() => { MoveTrainerAtDuelStart(); });
        }
    }

    void LoadSummonResources()
    {
        _ballSprite = Managers.Resource.Load<Sprite>("Sprite/pokeball/pb");
        _ballOpeningSprite = Managers.Resource.Load<Sprite>("Sprite/pokeball/pb_opening");
        _ballOpenSprite = Managers.Resource.Load<Sprite>("Sprite/pokeball/pb_open");

        Sprite[] burstSheet = Managers.Resource.LoadAll<Sprite>("Sprite/pokeball/Pokemon-Follow");
        _ballBurstSprites = new Sprite[]
        {
            System.Array.Find(burstSheet, sprite => sprite.name == "Pokemon-Follow_1"),
            System.Array.Find(burstSheet, sprite => sprite.name == "Pokemon-Follow_2"),
            System.Array.Find(burstSheet, sprite => sprite.name == "Pokemon-Follow_3"),
        };

        // 틴트 값이 서로 섞이지 않게 포켓몬마다 머티리얼을 따로 쓴다.
        Shader tintShader = Managers.Resource.Load<Shader>("Shaders/UI-PokemonTint");
        _myPokemonMaterial = new Material(tintShader);
        _enemyPokemonMaterial = new Material(tintShader);
        GetImage((int)Images.Image_MyPokemon).material = _myPokemonMaterial;
        GetImage((int)Images.Image_EnemyPokemon).material = _enemyPokemonMaterial;
    }

    void OnDestroy()
    {
        Destroy(_myPokemonMaterial);
        Destroy(_enemyPokemonMaterial);
        _battleAnimPlayer?.Dispose();
    }

    static float EaseInSine(float ratio) { return 1f - Mathf.Cos(ratio * Mathf.PI * 0.5f); }
    static float EaseInCubic(float ratio) { return ratio * ratio * ratio; }
    static float EaseOutCubic(float ratio) { return 1f - Mathf.Pow(1f - ratio, 3f); }

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
    }

    public void SetPlayerInfo()
    {
        GetText((int)Texts.Text_MyPokemonName).text = string.Empty;
        Sprite myTrainerImage = Managers.Resource.Load<Sprite>($"Sprite/character/trainer_f_back"); ;
        SetTrainerOrPokemonImage(myTrainerImage, true, true);

        GetText((int)Texts.Text_EnemyPokemonName).text = string.Empty;
        Sprite enemyTrainerImage = Managers.Resource.Load<Sprite>($"Sprite/character/rival_f 1");
        SetTrainerOrPokemonImage(enemyTrainerImage, false, true);
    }

    void FootHoldMoveAtDuelStart()
    {
        StartCoroutine(ImageHorizonMoveCoroutine(false, FootHoldMoveTime, _screenWidth, true));
    }

    void MoveTrainerAtDuelStart()
    {
        StartCoroutine(DuelStartSummonCoroutine());
    }

    IEnumerator DuelStartSummonCoroutine()
    {
        StartCoroutine(ImageHorizonMoveCoroutine(true, TrainerMoveTimeAtSpawnPokemon, _screenWidth * TrainerMoveDistanceRatio, false));

        // 트레이너가 화면 밖으로 나간 뒤에 착지하도록 상대는 조금 늦게 던진다.
        yield return new WaitForSeconds(EnemyThrowDelay);
        Coroutine enemySummon = StartCoroutine(SummonPokemonCoroutine(false));
        yield return new WaitForSeconds(PlayerThrowDelay - EnemyThrowDelay);
        yield return StartCoroutine(SummonPokemonCoroutine(true));
        yield return enemySummon;

        NewTurn();
        Managers.Job.Excute();
    }

    IEnumerator ImageHorizonMoveCoroutine(bool isTrainer, float moveTime, float moveDistance, bool setByHandler)
    {
        Image myImage = isTrainer ? GetImage((int)Images.Image_MyTrainer) : GetImage((int)Images.Image_My_FootHold);
        Image enemyImage = isTrainer ? GetImage((int)Images.Image_EnemyTrainer) : GetImage((int)Images.Image_Enemy_FootHold);

        Transform myTransform = myImage.transform;
        Transform enemyTransform = enemyImage.transform;

        Vector3 myTransformOriginPosition = myTransform.position;
        Vector3 enemyTransformOriginPosition = enemyTransform.position;

        if (isTrainer)
        {
            Animator animator = GetImage((int)Images.Image_MyTrainer).GetComponent<Animator>();
            if(animator != null)
                animator.Play("Trainer_ThrowPokeball");
        }
        else
        {
            myTransform.position = myTransformOriginPosition + new Vector3(moveDistance, 0f, 0f);
            enemyTransform.position = enemyTransformOriginPosition - new Vector3(moveDistance, 0f, 0f);
        }

        float accumulatedTime = 0f;
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

        // 트레이너는 몬스터볼이 땅에 닿기 전에 화면 밖으로 나간다. 제자리로 돌리면서 다시 보이지 않게 한다.
        if (isTrainer)
        {
            myImage.color = Color.clear;
            enemyImage.color = Color.clear;
        }

        myTransform.position = myTransformOriginPosition;
        enemyTransform.position = enemyTransformOriginPosition;

        if (setByHandler)
            Managers.Job.Excute();
    }

    public void ThrowPokeball(bool isMe)
    {
        CommonPlayerController targetPlayer = isMe ? Managers.Player.MyPlayer : Managers.Player.Enemy;
        string pokemonName = targetPlayer.GetCurPokemonName();
        string announce = isMe ? $"가라! {pokemonName}!" : $"상대는 {pokemonName}(을)를 내보냈다!";

        // 쓰러져서 교체할 때는 Excute가 C_TurnEnd를 보낸다. 다음 턴이 연출 도중에 시작되지 않게 소환이 끝난 뒤에 부른다.
        SetAnnounce(announce, false);
        StartCoroutine(ChangePokemonCoroutine(isMe));
    }

    IEnumerator ChangePokemonCoroutine(bool isMe)
    {
        yield return StartCoroutine(RecallPokemonCoroutine(isMe));
        yield return StartCoroutine(SummonPokemonCoroutine(isMe));

        Managers.Job.Excute();
    }

    IEnumerator RecallPokemonCoroutine(bool isMe)
    {
        Image pokemon = isMe ? GetImage((int)Images.Image_MyPokemon) : GetImage((int)Images.Image_EnemyPokemon);

        // 쓰러진 포켓몬은 이미 사라졌으므로 회수 연출 없이 바로 던진다.
        if (pokemon.enabled == false)
            yield break;

        Managers.Sound.Play("effect/pb_rel");
        yield return StartCoroutine(PokemonScaleTintCoroutine(pokemon, 1f, SummonStartScale, 0f, 1f));

        pokemon.enabled = false;
        pokemon.rectTransform.localScale = Vector3.one;
        SetPokemonTint(pokemon, 0f);

        yield return new WaitForSeconds(RecallToSummonDelay);
    }

    IEnumerator SummonPokemonCoroutine(bool isMe)
    {
        Image ball = isMe ? GetImage((int)Images.Image_MyPokeball) : GetImage((int)Images.Image_EnemyPokeball);
        Image pokemon = isMe ? GetImage((int)Images.Image_MyPokemon) : GetImage((int)Images.Image_EnemyPokemon);
        RectTransform ballRect = ball.rectTransform;
        RectTransform pokemonRect = pokemon.rectTransform;

        // 원작 좌표 (x: 시작 → 끝, y: 시작 → 정점 → 착지)
        float startX = isMe ? 36f : 248f;
        float landX = isMe ? 100f : 236f;
        float startY = isMe ? 80f : 44f;
        float peakY = isMe ? 70f : 34f;
        float landY = isMe ? 132f : 86f;

        Vector3 landPosition = pokemonRect.position;
        float unitX = Screen.width / OriginalScreenWidth;
        float unitY = Screen.height / OriginalScreenHeight;
        float startPositionX = landPosition.x + (startX - landX) * unitX;
        float startPositionY = landPosition.y + (landY - startY) * unitY;
        float peakPositionY = landPosition.y + (landY - peakY) * unitY;

        ball.sprite = _ballSprite;
        ball.color = Color.white;
        ballRect.localScale = Vector3.one;
        if (isMe)
            Managers.Sound.Play("effect/pb_throw");

        float accumulatedTime = 0f;
        while (accumulatedTime < BallFlightTime)
        {
            float positionY;
            float angle = 0f;
            if (accumulatedTime < BallRiseTime)
            {
                positionY = Mathf.Lerp(startPositionY, peakPositionY, EaseOutCubic(accumulatedTime / BallRiseTime));
            }
            else
            {
                float fallRatio = EaseInCubic((accumulatedTime - BallRiseTime) / BallFallTime);
                positionY = Mathf.Lerp(peakPositionY, landPosition.y, fallRatio);
                angle = -BallSpinAngle * fallRatio;
            }

            float positionX = Mathf.Lerp(startPositionX, landPosition.x, accumulatedTime / BallFlightTime);
            ballRect.position = new Vector3(positionX, positionY, landPosition.z);
            ballRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            accumulatedTime += Time.deltaTime;
            yield return null;
        }

        ballRect.position = landPosition;
        ballRect.localRotation = Quaternion.identity;

        Managers.Sound.Play("effect/pb_rel");
        SetBattlePokemonInfo(isMe);

        ball.sprite = _ballOpeningSprite;
        yield return new WaitForSeconds(BallOpenFrameTime);
        ball.sprite = _ballOpenSprite;
        yield return new WaitForSeconds(BallOpenFrameTime);

        ballRect.localScale = Vector3.one * BallBurstScale;
        for (int frame = 0; frame < _ballBurstSprites.Length; ++frame)
        {
            ball.sprite = _ballBurstSprites[frame];
            yield return new WaitForSeconds(BallBurstFrameTimes[frame]);
        }

        ball.color = Color.clear;
        ballRect.localScale = Vector3.one;
    }

    IEnumerator PokemonScaleTintCoroutine(Image pokemon, float fromScale, float toScale, float fromTint, float toTint)
    {
        // 원작은 발밑 기준으로 커지지만, 내 포켓몬은 아래쪽이 안내창에 가려져 이미지 중심을 기준으로 한다.
        Transform pokemonTransform = pokemon.transform;

        float accumulatedTime = 0f;
        while (accumulatedTime < SummonScaleTime)
        {
            float ratio = EaseInSine(accumulatedTime / SummonScaleTime);
            pokemonTransform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, ratio);
            SetPokemonTint(pokemon, Mathf.Lerp(fromTint, toTint, ratio));

            accumulatedTime += Time.deltaTime;
            yield return null;
        }

        pokemonTransform.localScale = Vector3.one * toScale;
        SetPokemonTint(pokemon, toTint);
    }

    void SetPokemonTint(Image pokemon, float amount)
    {
        Color tintColor = BallTintColor;
        tintColor.a = amount;
        pokemon.material.SetColor(TintColorId, tintColor);
    }

    public void SetBattlePokemonInfo(bool isMe)
    {
        TargetType targetType = isMe ? TargetType.Oneself : TargetType.Enemy;
        PokemonData targetPokemonData = isMe ? Managers.Player.MyPlayer.GetCurPokemonData() : Managers.Player.Enemy.GetCurPokemonData();

        SetPokemonHPBar(1f, targetType, false);
        SetPokemonLevel(isMe, targetPokemonData.Info.Level);

        TMP_Text targetText = isMe ? GetText((int)Texts.Text_MyPokemonName) : GetText((int)Texts.Text_EnemyPokemonName);
        targetText.text = targetPokemonData.Name;

        string pokemonImagePath = isMe ? $"Sprite/pokemon/back/{targetPokemonData.Id}" : $"Sprite/pokemon/{targetPokemonData.Id}";
        Sprite pokemonImage = Managers.Resource.LoadAll<Sprite>(pokemonImagePath)[0];
        SetTrainerOrPokemonImage(pokemonImage, isMe, false);
    }

    void SetPokemonLevel(bool isMe, int level)
    {
        Image level_000 = isMe ? GetImage((int)Images.Image_MyLevel_000) : GetImage((int)Images.Image_EnemyLevel_000);
        Image level_00 = isMe ? GetImage((int)Images.Image_MyLevel_00) : GetImage((int)Images.Image_EnemyLevel_00);
        Image level_0 = isMe ? GetImage((int)Images.Image_MyLevel_0) : GetImage((int)Images.Image_EnemyLevel_0);

        level_000.sprite = level == 100 ? _numberSprites[1] : null;
        level_000.color = level == 100 ? Color.white : Color.clear;

        int number_00 = (level % 100) / 10;
        level_00.sprite = _numberSprites[number_00];
        level_00.color = level >= 10 ? Color.white : Color.clear;

        int number_0 = level % 10;
        level_0.sprite = _numberSprites[number_0];
        level_0.color = Color.white;
    }

    public void SetTrainerOrPokemonImage(Sprite sprite, bool isMe, bool isTrainer)
    {
        Image targetImage = null;
        Image relativeImage = null;

        if (isTrainer)
        {
            targetImage = isMe ? GetImage((int)Images.Image_MyTrainer) : GetImage((int)Images.Image_EnemyTrainer);
            relativeImage = isMe ? GetImage((int)Images.Image_MyPokemon) : GetImage((int)Images.Image_EnemyPokemon);
            targetImage.color = Color.white;
            relativeImage.enabled = false;
        }
        else
        {
            targetImage = isMe ? GetImage((int)Images.Image_MyPokemon) : GetImage((int)Images.Image_EnemyPokemon);
            relativeImage = isMe ? GetImage((int)Images.Image_MyTrainer) : GetImage((int)Images.Image_EnemyTrainer);
            targetImage.enabled = true;
            relativeImage.color = Color.clear;

            StartCoroutine(SetSpawnPokemonScaleCoroutine(targetImage, isMe));
        }

        targetImage.sprite = sprite;
    }

    IEnumerator SetSpawnPokemonScaleCoroutine(Image image, bool isMe)
    {
        string pokemonId = isMe ? Managers.Player.MyPlayer.GetCurPokemonData().Id.ToString() : Managers.Player.Enemy.GetCurPokemonData().Id.ToString();
        Animator animator = image.GetComponent<Animator>();
        if (animator != null)
            animator.Play(pokemonId);

        yield return StartCoroutine(PokemonScaleTintCoroutine(image, SummonStartScale, 1f, 1f, 0f));

        Managers.Sound.Play($"cry/{pokemonId}");
    }

    public void NewTurn()
    {
        string anounceText = string.Empty;
        if (Managers.Player.MyTurn)
        {
            Managers.UI.ShowPopupUI<UIBattleBehaviorSelectPopup>();
            string myPokemonName = Managers.Data.PokeonDict[Managers.Player.MyPlayer.GetCurPokemonData().Id].Name;
            anounceText = $"{myPokemonName}(은)는 무엇을 할까?";
        }
        else
        {
            anounceText = "상대 차례를 기다리는 중";
        }

        Managers.Job.Push(() => SetAnnounce(anounceText, true));         
    }

    public void ChangeFalldownPokemon(bool isMe, bool isDuelEnd)
    {
        StartCoroutine(FallDownEffect(isMe, isDuelEnd));
    }

    public IEnumerator FallDownEffect(bool isMe, bool isDuelEnd)
    {
        Image targetImage = isMe ? GetImage((int)Images.Image_MyPokemon) : GetImage((int)Images.Image_EnemyPokemon);
        string curPokemonId = isMe ? Managers.Player.MyPlayer.GetCurPokemonData().Id.ToString() : Managers.Player.Enemy.GetCurPokemonData().Id.ToString();
        Managers.Sound.Play($"cry/{curPokemonId}", Define.Sound.Effect, FaintCryPitch);

        AudioClip cryClip = Managers.Resource.Load<AudioClip>($"Sounds/cry/{curPokemonId}");
        if (cryClip != null)
            yield return new WaitForSeconds(Mathf.Min(cryClip.length / FaintCryPitch, FaintCryMaxTime));

        // 내려간 만큼 아래쪽을 잘라 땅 아래로 가라앉는 것처럼 보이게 한다.
        RectTransform targetRect = targetImage.rectTransform;
        Vector2 originPosition = targetRect.anchoredPosition;
        targetImage.type = Image.Type.Filled;
        targetImage.fillMethod = Image.FillMethod.Vertical;
        targetImage.fillOrigin = (int)Image.OriginVertical.Top;

        float accumulateTime = 0f;
        while (accumulateTime < FaintTime)
        {
            float ratio = EaseInSine(accumulateTime / FaintTime);
            targetImage.fillAmount = 1f - ratio;
            targetRect.anchoredPosition = originPosition + Vector2.down * (targetRect.rect.height * ratio);

            accumulateTime += Time.deltaTime;
            yield return null;
        }

        targetImage.enabled = false;
        targetImage.type = Image.Type.Simple;
        targetImage.fillAmount = 1f;
        targetRect.anchoredPosition = originPosition;

        if (isDuelEnd)
        {
            Managers.Job.Excute();
        }
        else if (isMe)
        {
            UIBattlePokemonChangePopup battlePokemonChangePopup = Managers.UI.ShowPopupUI<UIBattlePokemonChangePopup>();
            battlePokemonChangePopup.BattleScene = this;
            battlePokemonChangePopup.MustChange = true;
        }
    }

    public void DuelEnd(bool isWin, bool isRunaway)
    {
        string text = isWin ? (isRunaway ? $"{Managers.Player.Enemy.Name}(은)는 도망쳤다!" : $"{Managers.Player.Enemy.Name}(와)과의 대결에서 승리했다!") 
            : (isRunaway ? "무사히 도망쳤다!" :$"{Managers.Player.MyPlayer.Name}(은)는 눈 앞이 캄캄해졌다.");
        Managers.Job.Push(() => SetAnnounce(text, true) );
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
    
    public void SetPokemonHPBar(float targetRatio, TargetType targetType, bool setByHandler = true)
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
            if (hpState < 0 || hpState > 2)
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

    // 기술 연출을 재생한다. hitSoundPath가 null이면 피해를 주지 않은 경우라 깜빡임과 소리를 생략한다.
    public void PlaySkillAnim(int skillId, bool isAttackerMe, bool isSelfTarget, string hitSoundPath)
    {
        StartCoroutine(SkillAnimCoroutine(skillId, isAttackerMe, isSelfTarget, hitSoundPath));
    }

    IEnumerator SkillAnimCoroutine(int skillId, bool isAttackerMe, bool isSelfTarget, string hitSoundPath)
    {
        yield return StartCoroutine(_battleAnimPlayer.Play(skillId, isAttackerMe, isSelfTarget));

        if (hitSoundPath != null)
        {
            Image targetImage = GetImage(isAttackerMe ? (int)Images.Image_EnemyPokemon : (int)Images.Image_MyPokemon);
            Managers.Sound.Play(hitSoundPath);

            for (int toggle = 0; toggle < HitFlashToggleCount; ++toggle)
            {
                targetImage.enabled = toggle % 2 == 1;
                yield return new WaitForSeconds(HitFlashInterval);
            }

            targetImage.enabled = true;
        }

        Managers.Job.Excute();
    }
}
