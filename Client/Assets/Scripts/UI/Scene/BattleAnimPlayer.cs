using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// 필드 이름은 원작(PokeRogue) battle-anims JSON과 같아야 한다.
public class BattleAnimFrame
{
    public float x;
    public float y;
    public float zoomX;
    public float zoomY;
    public float angle;
    public bool mirror;
    public bool visible;
    public bool locked;
    public float opacity;
    public int blendType;
    public int target;
    public int graphicFrame;
    public int priority;
    public int focus = BattleAnimPlayer.FocusTarget;
    public float[] tone;
}

public class BattleAnimEvent
{
    public string eventType;
    public string resourceName;
    public float volume = 100f;
    public float pitch = 100f;
    public float? bgX;
    public float? bgY;
    public float? opacity;
    public float duration;
}

public class BattleAnimConfig
{
    public string graphic;
    public List<List<BattleAnimFrame>> frames;
    public Dictionary<int, List<BattleAnimEvent>> frameTimedEvents;

    [JsonIgnore] public bool IsOppAnim;
}

// 원작 battle-anims.ts의 BattleAnim.play()를 uGUI로 옮긴 재생기.
// 재생 중에는 원작처럼 실제 포켓몬을 숨기고, 복사본과 이펙트를 프레임 데이터대로 그린다.
public class BattleAnimPlayer
{
    public const int FocusTarget = 1;
    const int FocusUser = 2;
    const int FocusUserTarget = 3;

    const int FrameTargetUser = 0;
    const int FrameTargetTarget = 1;

    const int BlendAdd = 1;

    const float TickTime = 1f / 60f;
    const float FrameTime = TickTime * 3f; // 원작 getFrameMs(3)
    const float SoundTickTime = 0.03333f;

    // 원작 화면의 기준 위치: 내 포켓몬 중심(106, 116)에서 상대 포켓몬 중심(234, 52)까지의 거리
    const float FocusDistanceX = 128f;
    const float FocusDistanceY = 64f;

    const float GraphicCellSize = 96f;
    const float FieldWidth = 320f;
    const float FieldHeight = 180f;
    const float BgTileScale = 1.25f;
    const float BgOriginX = 320f;
    const float BgOriginY = 284f;

    // 원작 포켓몬의 대표 너비. 이 프로젝트의 포켓몬 크기에 맞춰 이펙트 크기를 정한다.
    const float OriginalPokemonWidth = 64f;

    static readonly int ToneId = Shader.PropertyToID("_Tone");
    static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    class AnimSprite
    {
        public RectTransform Rect;
        public Graphic Graphic;
        public Material Material;
        public int BlendType = -1;
        public bool Locked;
    }

    readonly Dictionary<(int, int), BattleAnimConfig> _configs = new Dictionary<(int, int), BattleAnimConfig>();
    readonly List<Material> _materials = new List<Material>();

    readonly Image _myPokemon;
    readonly Image _enemyPokemon;
    readonly Shader _shader;

    readonly GameObject _layer;
    readonly RawImage _background;
    readonly RectTransform _backLayer;
    readonly RectTransform _frontLayer;
    readonly RectTransform _pokemonLayer;
    readonly Transform _soundRoot;

    readonly List<AnimSprite> _activeGraphics = new List<AnimSprite>();
    readonly List<AnimSprite> _activeUserSprites = new List<AnimSprite>();
    readonly List<AnimSprite> _activeTargetSprites = new List<AnimSprite>();
    readonly Stack<AnimSprite> _freeGraphics = new Stack<AnimSprite>();
    readonly Stack<AnimSprite> _freePokemonSprites = new Stack<AnimSprite>();

    Vector2 _userCenter;
    Vector2 _targetCenter;
    Vector2 _myCenter;
    Vector2 _enemyCenter;
    float _unit;
    bool _isPlaying;

    Texture2D _graphicTexture;
    int _graphicColumns;
    int _graphicRows;

    bool _hasBackground;
    Vector2 _bgPosition;
    float _bgAlpha;
    bool _bgTweening;
    float _bgTweenStart;
    float _bgTweenTime;
    Vector2 _bgFromPosition;
    Vector2 _bgToPosition;
    float _bgFromAlpha;
    float _bgToAlpha;

    public BattleAnimPlayer(Image myPokemon, Image enemyPokemon, RectTransform fieldRoot, Transform soundRoot)
    {
        _myPokemon = myPokemon;
        _enemyPokemon = enemyPokemon;
        _soundRoot = soundRoot;
        _shader = Managers.Resource.Load<Shader>("Shaders/UI-BattleAnim");

        // 발판과 포켓몬 위, 상태창과 안내창 아래에 그린다.
        RectTransform layerRect = CreateLayer("BattleAnimLayer", fieldRoot);
        _layer = layerRect.gameObject;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        Stretch(backgroundRect, layerRect);
        _background = backgroundObject.GetComponent<RawImage>();
        _background.raycastTarget = false;
        _background.material = CreateMaterial();

        _backLayer = CreateLayer("Back", layerRect);
        _pokemonLayer = CreateLayer("Pokemon", layerRect);
        _frontLayer = CreateLayer("Front", layerRect);

        _layer.SetActive(false);
    }

    public void Dispose()
    {
        foreach (Material material in _materials)
            Object.Destroy(material);
        _materials.Clear();
    }

    static RectTransform CreateLayer(string name, RectTransform parent)
    {
        GameObject layerObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = layerObject.GetComponent<RectTransform>();
        Stretch(rect, parent);
        return rect;
    }

    static void Stretch(RectTransform rect, RectTransform parent)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    Material CreateMaterial()
    {
        Material material = new Material(_shader);
        _materials.Add(material);
        return material;
    }

    BattleAnimConfig LoadConfig(int skillId, bool isAttackerMe)
    {
        // 원작 JSON은 상대가 쓸 때의 연출이 따로 있으면 두 항목을 가진 배열이다.
        int variant = isAttackerMe ? 0 : 1;
        if (_configs.TryGetValue((skillId, variant), out BattleAnimConfig config))
            return config;

        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Data/BattleAnims/{skillId}");
        if (textAsset == null)
        {
            Debug.LogError($"기술 연출 데이터가 없습니다: {skillId}");
            return null;
        }

        JToken token = JToken.Parse(textAsset.text);
        if (token is JArray array)
        {
            config = array[variant].ToObject<BattleAnimConfig>();
            config.IsOppAnim = variant == 1;
        }
        else
        {
            config = token.ToObject<BattleAnimConfig>();
        }

        _configs.Add((skillId, variant), config);
        return config;
    }

    public IEnumerator Play(int skillId, bool isAttackerMe, bool isSelfTarget)
    {
        BattleAnimConfig config = LoadConfig(skillId, isAttackerMe);
        if (config == null)
            yield break;

        // 작업 큐가 앞당겨지면 연출이 겹칠 수 있다. 숨긴 포켓몬을 원래 상태로 착각하지 않게 앞 연출이 끝나길 기다린다.
        while (_isPlaying)
            yield return null;
        _isPlaying = true;

        Image attacker = isAttackerMe ? _myPokemon : _enemyPokemon;
        Image defender = isSelfTarget ? attacker : (isAttackerMe ? _enemyPokemon : _myPokemon);

        // 상대용 연출은 내 포켓몬을 USER로 두고 만들어져 있어 원작처럼 둘을 맞바꾼다.
        Image user = config.IsOppAnim ? defender : attacker;
        Image target = config.IsOppAnim ? attacker : defender;

        _myCenter = GetCenter(_myPokemon);
        _enemyCenter = GetCenter(_enemyPokemon);
        _userCenter = GetCenter(user);
        _targetCenter = GetCenter(target);
        _unit = _enemyPokemon.rectTransform.rect.width / OriginalPokemonWidth;

        LoadGraphic(config.graphic);

        bool userEnabled = user.enabled;
        bool targetEnabled = target.enabled;

        _layer.SetActive(true);
        _background.enabled = false;
        _hasBackground = false;
        _bgTweening = false;

        // 효과음이 프레임보다 길면 그만큼 더 기다린다. 단위는 원작과 같은 1/60초다.
        int remainTicks = config.frames.Count;

        yield return Wait(FrameTime);

        for (int frameIndex = 0; frameIndex < config.frames.Count; ++frameIndex)
        {
            if (frameIndex == 0)
            {
                user.enabled = false;
                target.enabled = false;
            }

            RenderFrame(config.frames[frameIndex], user, target, userEnabled, targetEnabled);

            if (config.frameTimedEvents != null && config.frameTimedEvents.TryGetValue(frameIndex, out List<BattleAnimEvent> events))
            {
                int baseTicks = config.frames.Count - frameIndex;
                foreach (BattleAnimEvent animEvent in events)
                    remainTicks = Mathf.Max(baseTicks + ExecuteEvent(animEvent), remainTicks);
            }

            --remainTicks;
            yield return Wait(FrameTime);
        }

        // 원작은 효과음이 끝날 때까지 포켓몬도 숨겨 둔다. 꼬리가 길면 깜빡여 보여 프레임이 끝나면 바로 되돌린다.
        ClearSprites();
        user.enabled = userEnabled;
        target.enabled = targetEnabled;

        if (remainTicks > 0)
            yield return Wait(remainTicks * TickTime);

        _background.enabled = false;
        _layer.SetActive(false);
        _isPlaying = false;
    }

    static Vector2 GetCenter(Image image)
    {
        RectTransform rect = image.rectTransform;
        return rect.TransformPoint(rect.rect.center);
    }

    void LoadGraphic(string graphic)
    {
        _graphicTexture = string.IsNullOrEmpty(graphic) ? null : Managers.Resource.Load<Texture2D>($"Sprite/battle_anims/{graphic}");
        if (_graphicTexture == null)
        {
            if (string.IsNullOrEmpty(graphic) == false)
                Debug.LogError($"기술 연출 이미지가 없습니다: {graphic}");
            return;
        }

        _graphicColumns = Mathf.RoundToInt(_graphicTexture.width / GraphicCellSize);
        _graphicRows = Mathf.RoundToInt(_graphicTexture.height / GraphicCellSize);
    }

    IEnumerator Wait(float seconds)
    {
        float endTime = Time.time + seconds;
        while (Time.time < endTime)
        {
            UpdateBackground();
            yield return null;
        }
    }

    void RenderFrame(List<BattleAnimFrame> frames, Image user, Image target, bool userEnabled, bool targetEnabled)
    {
        int graphicCount = 0;
        int userCount = 0;
        int targetCount = 0;

        foreach (BattleAnimFrame frame in frames)
        {
            if (frame.target == FrameTargetUser || frame.target == FrameTargetTarget)
            {
                bool isUser = frame.target == FrameTargetUser;
                if (isUser && user == target)
                    continue;

                List<AnimSprite> sprites = isUser ? _activeUserSprites : _activeTargetSprites;
                int index = isUser ? userCount++ : targetCount++;
                if (index == sprites.Count)
                    sprites.Add(GetPokemonSprite());

                ApplyPokemonFrame(sprites[index], frame, isUser ? user : target, isUser ? userEnabled : targetEnabled);
            }
            else
            {
                int index = graphicCount++;
                if (index == _activeGraphics.Count)
                    _activeGraphics.Add(GetGraphicSprite());

                ApplyGraphicFrame(_activeGraphics[index], frame);
            }
        }

        ReleaseUnused(_activeGraphics, graphicCount, _freeGraphics);
        ReleaseUnused(_activeUserSprites, userCount, _freePokemonSprites);
        ReleaseUnused(_activeTargetSprites, targetCount, _freePokemonSprites);
    }

    // 이번 프레임에 쓰이지 않은 스프라이트는 없앤다. 다만 locked인 포켓몬은 그대로 둔다.
    static void ReleaseUnused(List<AnimSprite> sprites, int usedCount, Stack<AnimSprite> freeSprites)
    {
        for (int i = sprites.Count - 1; i >= usedCount; --i)
        {
            if (sprites[i].Locked)
                continue;

            sprites[i].Rect.gameObject.SetActive(false);
            freeSprites.Push(sprites[i]);
            sprites.RemoveAt(i);
        }
    }

    void ClearSprites()
    {
        foreach (AnimSprite sprite in _activeGraphics)
        {
            sprite.Rect.gameObject.SetActive(false);
            _freeGraphics.Push(sprite);
        }
        _activeGraphics.Clear();

        foreach (List<AnimSprite> sprites in new[] { _activeUserSprites, _activeTargetSprites })
        {
            foreach (AnimSprite sprite in sprites)
            {
                sprite.Locked = false;
                sprite.Rect.gameObject.SetActive(false);
                _freePokemonSprites.Push(sprite);
            }
            sprites.Clear();
        }
    }

    AnimSprite GetGraphicSprite()
    {
        AnimSprite sprite = _freeGraphics.Count > 0 ? _freeGraphics.Pop() : CreateSprite<RawImage>("Graphic", _frontLayer);
        sprite.Rect.gameObject.SetActive(true);
        return sprite;
    }

    AnimSprite GetPokemonSprite()
    {
        AnimSprite sprite = _freePokemonSprites.Count > 0 ? _freePokemonSprites.Pop() : CreateSprite<Image>("Pokemon", _pokemonLayer);
        sprite.Rect.gameObject.SetActive(true);
        return sprite;
    }

    AnimSprite CreateSprite<T>(string name, RectTransform parent) where T : Graphic
    {
        GameObject spriteObject = new GameObject(name, typeof(RectTransform), typeof(T));
        RectTransform rect = spriteObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;

        Graphic graphic = spriteObject.GetComponent<T>();
        graphic.raycastTarget = false;
        graphic.material = CreateMaterial();

        return new AnimSprite { Rect = rect, Graphic = graphic, Material = graphic.material };
    }

    void ApplyPokemonFrame(AnimSprite sprite, BattleAnimFrame frame, Image source, bool sourceEnabled)
    {
        Image image = (Image)sprite.Graphic;
        // 대기 애니메이션을 따라간다. 스프라이트가 빈 키에서는 흰 사각형이 되지 않게 이전 모습을 유지한다.
        if (source.sprite != null)
            image.sprite = source.sprite;
        image.preserveAspect = source.preserveAspect;
        image.enabled = frame.visible && sourceEnabled;
        sprite.Rect.sizeDelta = source.rectTransform.rect.size;
        sprite.Locked = frame.locked;

        ApplyTransform(sprite, frame, false);
        SetColor(sprite, frame);
        SetBlend(sprite, frame.blendType);

        Vector4 tone = Vector4.zero;
        if (frame.tone != null && frame.tone.Length == 4)
            tone = new Vector4(frame.tone[0], frame.tone[1], frame.tone[2], frame.tone[3]) / 255f;
        sprite.Material.SetVector(ToneId, tone);
    }

    void ApplyGraphicFrame(AnimSprite sprite, BattleAnimFrame frame)
    {
        RawImage image = (RawImage)sprite.Graphic;
        bool hasCell = _graphicTexture != null && frame.graphicFrame >= 0 && frame.graphicFrame < _graphicColumns * _graphicRows;
        image.enabled = frame.visible && hasCell;
        if (hasCell == false)
            return;

        image.texture = _graphicTexture;
        int column = frame.graphicFrame % _graphicColumns;
        int row = frame.graphicFrame / _graphicColumns;
        float cellWidth = 1f / _graphicColumns;
        float cellHeight = 1f / _graphicRows;
        image.uvRect = new Rect(column * cellWidth, 1f - (row + 1) * cellHeight, cellWidth, cellHeight);

        float size = GraphicCellSize * _unit;
        sprite.Rect.sizeDelta = new Vector2(size, size);

        // 포켓몬 앞뒤 순서 (원작 setSpritePriority). 숨긴 실제 포켓몬을 기준으로 옮기므로 복사본보다 뒤에 놓인다.
        bool hasFocusPokemon = frame.focus == FocusUser || frame.focus == FocusTarget;
        bool isBehind = frame.priority == 0 || (frame.priority == 2 && (_hasBackground || hasFocusPokemon)) || (frame.priority == 3 && hasFocusPokemon);
        Transform parent = isBehind ? _backLayer : _frontLayer;
        if (sprite.Rect.parent != parent)
            sprite.Rect.SetParent(parent, false);
        sprite.Rect.SetAsLastSibling();

        ApplyTransform(sprite, frame, true);
        SetColor(sprite, frame);
        SetBlend(sprite, frame.blendType);
    }

    void ApplyTransform(AnimSprite sprite, BattleAnimFrame frame, bool isGraphic)
    {
        Vector2 position;
        float scaleX = frame.zoomX / 100f * (frame.mirror ? -1f : 1f);
        float scaleY = frame.zoomY / 100f;

        switch (frame.focus)
        {
            case FocusTarget:
                position = _targetCenter + new Vector2(frame.x - FocusDistanceX, -(frame.y + FocusDistanceY)) * _unit;
                break;
            case FocusUser:
                position = _userCenter + new Vector2(frame.x, -frame.y) * _unit;
                break;
            case FocusUserTarget:
                position = TransformPoint(_userCenter, _targetCenter, frame.x, frame.y);
                // 원작 데이터는 왼쪽(내 포켓몬)에서 오른쪽으로 쏘는 기준이라 반대 방향이면 뒤집는다.
                if (isGraphic && _userCenter.x > _targetCenter.x)
                    scaleX *= -1f;
                break;
            default:
                position = TransformPoint(_myCenter, _enemyCenter, frame.x, frame.y);
                break;
        }

        Vector3 worldPosition = position;
        worldPosition.z = _layer.transform.position.z;
        sprite.Rect.position = worldPosition;
        sprite.Rect.localRotation = Quaternion.Euler(0f, 0f, frame.angle);
        sprite.Rect.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    // 원작 transformPoint: 기준 선분 위의 비율을 실제 두 포켓몬 사이로 옮긴다. 화면 배치가 달라도 대상에 정확히 닿는다.
    static Vector2 TransformPoint(Vector2 from, Vector2 to, float x, float y)
    {
        return new Vector2(from.x + x / FocusDistanceX * (to.x - from.x), from.y - y / FocusDistanceY * (to.y - from.y));
    }

    static void SetColor(AnimSprite sprite, BattleAnimFrame frame)
    {
        sprite.Graphic.color = new Color(1f, 1f, 1f, frame.opacity / 255f);
    }

    static void SetBlend(AnimSprite sprite, int blendType)
    {
        if (sprite.BlendType == blendType)
            return;

        sprite.BlendType = blendType;
        BlendMode source = BlendMode.One;
        BlendMode destination = BlendMode.OneMinusSrcAlpha;
        if (blendType == BlendAdd)
        {
            destination = BlendMode.One;
        }
        else if (blendType > BlendAdd)
        {
            source = BlendMode.OneMinusDstColor;
            destination = BlendMode.OneMinusSrcColor;
        }

        sprite.Material.SetInt(SrcBlendId, (int)source);
        sprite.Material.SetInt(DstBlendId, (int)destination);
    }

    // 다음 동작까지 기다릴 틱 수를 준다.
    int ExecuteEvent(BattleAnimEvent animEvent)
    {
        switch (animEvent.eventType)
        {
            case "AnimTimedSoundEvent":
                return PlaySound(animEvent);
            case "AnimTimedAddBgEvent":
                AddBackground(animEvent);
                return Mathf.RoundToInt(animEvent.duration) * 2;
            case "AnimTimedUpdateBgEvent":
                UpdateBackgroundTween(animEvent);
                return Mathf.RoundToInt(animEvent.duration) * 2;
        }
        return 0;
    }

    int PlaySound(BattleAnimEvent animEvent)
    {
        if (string.IsNullOrEmpty(animEvent.resourceName))
            return 0;

        string path = $"Sounds/battle_anims/{Path.GetFileNameWithoutExtension(animEvent.resourceName)}";
        AudioClip clip = Managers.Resource.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogError($"기술 연출 효과음이 없습니다: {path}");
            return 0;
        }

        // 한 기술 안에서 음높이가 다른 소리가 겹치므로 공용 AudioSource 대신 소리마다 따로 쓴다.
        float pitch = animEvent.pitch * 0.01f;
        GameObject soundObject = new GameObject("BattleAnimSound");
        soundObject.transform.SetParent(_soundRoot, false);
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.pitch = pitch;
        audioSource.volume = animEvent.volume * 0.01f;
        audioSource.Play();
        Object.Destroy(soundObject, clip.length / pitch + 0.1f);

        return Mathf.CeilToInt(clip.length / SoundTickTime);
    }

    void AddBackground(BattleAnimEvent animEvent)
    {
        Texture texture = Texture2D.blackTexture;
        if (string.IsNullOrEmpty(animEvent.resourceName) == false)
            texture = Managers.Resource.Load<Texture2D>($"Sprite/battle_anims/{animEvent.resourceName}");
        if (texture == null)
        {
            Debug.LogError($"기술 연출 배경이 없습니다: {animEvent.resourceName}");
            return;
        }

        _background.texture = texture;
        _background.enabled = true;
        _hasBackground = true;
        _bgTweening = false;
        _bgPosition = new Vector2((animEvent.bgX ?? 0f) - BgOriginX, (animEvent.bgY ?? 0f) - BgOriginY);
        _bgAlpha = (animEvent.opacity ?? 0f) / 255f;
        ApplyBackground();
    }

    void UpdateBackgroundTween(BattleAnimEvent animEvent)
    {
        if (_hasBackground == false)
            return;

        _bgFromPosition = _bgPosition;
        _bgFromAlpha = _bgAlpha;
        _bgToPosition = new Vector2(
            animEvent.bgX.HasValue ? animEvent.bgX.Value * 0.5f - BgOriginX : _bgPosition.x,
            animEvent.bgY.HasValue ? animEvent.bgY.Value * 0.5f - BgOriginY : _bgPosition.y);
        _bgToAlpha = animEvent.opacity.HasValue ? animEvent.opacity.Value / 255f : _bgAlpha;
        _bgTweenStart = Time.time;
        _bgTweenTime = animEvent.duration * FrameTime;
        _bgTweening = true;
    }

    void UpdateBackground()
    {
        if (_bgTweening == false)
            return;

        float ratio = _bgTweenTime > 0f ? Mathf.Clamp01((Time.time - _bgTweenStart) / _bgTweenTime) : 1f;
        _bgPosition = Vector2.Lerp(_bgFromPosition, _bgToPosition, ratio);
        _bgAlpha = Mathf.Lerp(_bgFromAlpha, _bgToAlpha, ratio);
        ApplyBackground();

        if (ratio >= 1f)
            _bgTweening = false;
    }

    void ApplyBackground()
    {
        float tileWidth = _background.texture.width * BgTileScale;
        float tileHeight = _background.texture.height * BgTileScale;
        float top = -_bgPosition.y / tileHeight;
        float height = FieldHeight / tileHeight;
        _background.uvRect = new Rect(-_bgPosition.x / tileWidth, 1f - (top + height), FieldWidth / tileWidth, height);
        _background.color = new Color(1f, 1f, 1f, _bgAlpha);
    }
}
