using UnityEngine;
using UnityEngine.UI;

public class SpawnPokemonAnimationScript : StateMachineBehaviour
{
    Image _imageComponent = null;

    [SerializeField] 
    bool _isMe = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_imageComponent == null)
            _imageComponent = animator.GetComponent<Image>();
        
        _imageComponent.color = Color.white;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        battleScene.SetBattlePokemonInfo(_isMe);
    }

    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //}

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _imageComponent.color = Color.clear;
    }
}
