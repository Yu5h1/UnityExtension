using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

public class StateBehaviour : StateMachineBehaviour
{
    public AnimationController Controller { get; private set; }

    [SerializeField] private UnityEvent<StateBehaviour> _entered;
    public event UnityAction<StateBehaviour> entered
    {
        add => _entered.AddListener(value);
        remove => _entered.RemoveListener(value);
    }
    [SerializeField] private UnityEvent<StateBehaviour> _exited;
    public event UnityAction<StateBehaviour> exited
    {
        add => _exited.AddListener(value);
        remove => _exited.RemoveListener(value);
    }
    //public enum Phase
    //{ 
    //    Enter,
    //    Update,
    //    Exit
    //}


    private void Init(AnimationController animationController)
    {
        name = name.TrimEnd("(Clone)");
        Controller = animationController;
        Controller.Join(this);
    }

    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Controller == null)
            Init(animator.GetComponent<AnimationController>());
        _entered?.Invoke(this);
    }

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Controller == null)
            Controller = animator.GetComponent<AnimationController>();
        _exited?.Invoke(this);
    }

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    //override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    //override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}

    public void SetBool(string name, bool value) => Controller.animator.SetBool(name, value);
    public bool GetBool(string name, bool value) => Controller.animator.GetBool(name);
    public void Test() => $"IsNull :{(Controller == null)}".print();
}
