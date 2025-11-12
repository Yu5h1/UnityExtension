using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class Group<T> : BaseMonoBehaviour where T : Component
    {
        [SerializeField,ReadOnly]
        protected T[] members;

        protected abstract void EnableMembers();
        protected abstract void DisableMembers();
    }

    public class BehaviourGroup<T> : Group<T> where T : Behaviour
    {

        protected override void OnInitializing()
        {
            members = GetComponentsInChildren<T>();
        }

        protected override void DisableMembers() => SetMembersEnable(false);
        protected override void EnableMembers() => SetMembersEnable(true);

        public void SetMembersEnable(bool enable)
        {
            for (int i = 0; i < members.Length; i++)
                members[i].enabled = enable;
        }
    }

}
