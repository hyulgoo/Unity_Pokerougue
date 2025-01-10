using UnityEngine;
using UnityEngine.UI;

public class HitAnimationScript : StateMachineBehaviour
{
    Image _imageComponent = null;
    [SerializeField]
    bool _isMe;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _imageComponent = animator.GetComponent<Image>();
        if (_imageComponent == null)
            Debug.Assert(false, "Image Component를 찾을 수 없습니다.");
        
        string pokemonSpritePath = _isMe ? $"Sprite/pokemon/back/" + Managers.Player.MyPlayer.GetCurPokemonData().Id.ToString() : $"Sprite/pokemon/" + Managers.Player.Enemy.GetCurPokemonData().Id.ToString();
        Sprite pokemonSprite = Managers.Resource.LoadAll<Sprite>(pokemonSpritePath)[0];
        if (pokemonSprite == null)
            Debug.Assert(false, $"Sprite({pokemonSpritePath})를 찾을 수 없습니다.");
        _imageComponent.sprite = pokemonSprite;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        string pokemonStateName = _isMe ? Managers.Player.MyPlayer.GetCurPokemonData().Id.ToString() : Managers.Player.Enemy.GetCurPokemonData().Id.ToString();
        animator.Play(pokemonStateName);
        _imageComponent = null;
    }
}
