using UnityEngine;
using UnityEngine.UI;
using static UIBattleScene;

public class AttackAnimationScript : StateMachineBehaviour
{
    Image imageComponent = null;
    [SerializeField]
    bool _isMe = false;
    bool _isAttack = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        imageComponent = animator.GetComponent<Image>();
        if (imageComponent == null)
            Debug.Assert(false, "Image Component를 찾을 수 없습니다.");

        string pokemonSpritePath = _isMe ? $"Sprite/pokemon/back/" + Managers.Player.MyPlayer.GetCurPokemonData().Id.ToString() : $"Sprite/pokemon/" + Managers.Player.Enemy.GetCurPokemonData().Id.ToString();
        Sprite pokemonSprite = Managers.Resource.LoadAll<Sprite>(pokemonSpritePath)[0];
        if (pokemonSprite == null)
            Debug.Assert(false, $"Sprite({pokemonSpritePath})를 찾을 수 없습니다.");
        imageComponent.sprite = pokemonSprite;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_isAttack || stateInfo.normalizedTime < 0.4f)
            return;
        _isAttack = true;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene != null)
        {
            UIBattleScene.Images targetImage = Managers.Player.MyTurn ? UIBattleScene.Images.Image_EnemyPokemon : UIBattleScene.Images.Image_MyPokemon; 
            battleScene.PlayAnimationInBattleScene(targetImage, "Hit");
            Managers.Sound.Play("effect/hit");
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        string pokemonStateName = _isMe ? Managers.Player.MyPlayer.GetCurPokemonData().Id.ToString() : Managers.Player.Enemy.GetCurPokemonData().Id.ToString();
        animator.Play(pokemonStateName);
        imageComponent = null;
        _isAttack = false;
    }
}
