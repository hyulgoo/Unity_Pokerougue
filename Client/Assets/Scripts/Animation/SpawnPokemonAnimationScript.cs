using UnityEngine;
using UnityEngine.UI;

public class SpawnPokemonAnimationScript : StateMachineBehaviour
{
    Image _imageComponent = null;
    [SerializeField] 
    bool isMe = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_imageComponent == null)
        { 
            _imageComponent = animator.GetComponent<Image>();
            _imageComponent.color = Color.white;
        }

        _imageComponent.color = Color.clear;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene != null)
        {
            battleScene.SetBattlePokemonInfo(isMe, false);
        }
    }

    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //}

    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
