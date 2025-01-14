using UnityEngine;
using UnityEngine.UI;

public class SpaAttackAnimationScript : StateMachineBehaviour
{
    Image _imageComponent = null;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_imageComponent == null)
            _imageComponent = animator.GetComponent<Image>();
        
        _imageComponent.color = Color.white;
    }

    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //}

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _imageComponent.color = Color.clear;
    }
}
