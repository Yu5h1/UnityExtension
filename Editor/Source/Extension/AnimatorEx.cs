using System.ComponentModel;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class AnimatorEx
    {
        static int GetCapsuleColliderDirection(Vector3 vector) {
            if (vector.x != 0) return 0;
            if (vector.y != 0) return 1;
            return 2;
        }
        [MenuItem("CONTEXT/Animator/Reset Pose")]
        public static void ResetPose(MenuCommand command)
        {
            Animator animator = (Animator)command.context;
            foreach (var bone in animator.GetHumanBodyBones())
            {
                new SerializedObject(bone).Revert("m_LocalRotation");
            }
        }
        [MenuItem("CONTEXT/Animator/Assign RagDoll Wizard")]
        public static void AssignRagDollWizardByAnimator(MenuCommand command)
        {
            Animator animator = (Animator)command.context;
            if (animator.isHuman)
            {
                var RagdollBuilderType = typeof(Editor).Assembly.GetType("UnityEditor.RagdollBuilder");
                var results = Resources.FindObjectsOfTypeAll(RagdollBuilderType);

                if (results.Length == 0)
                {
                    EditorApplication.ExecuteMenuItem("GameObject/3D Object/Ragdoll...");
                    results = Resources.FindObjectsOfTypeAll(RagdollBuilderType);
                }

                var RagDollWizardWindow = results[0];

                int bodylayer = LayerMask.NameToLayer("Body");
                System.Action<string, HumanBodyBones> SetTransformField = (name, val) =>
                {
                    var transform = animator.GetBoneTransform(val);
                    if (transform != null) {
                        if (bodylayer > -1) transform.gameObject.layer = bodylayer;
                        RagdollBuilderType.GetField(name, BindingFlags.Public | BindingFlags.Instance).SetValue(RagDollWizardWindow, transform);
                    }else $"{val} transform does not exist.".print();
                };

                SetTransformField("pelvis", HumanBodyBones.Hips);
                SetTransformField("leftHips", HumanBodyBones.LeftUpperLeg);
                SetTransformField("leftKnee", HumanBodyBones.LeftLowerLeg);
                SetTransformField("leftFoot", HumanBodyBones.LeftFoot);
                SetTransformField("rightHips", HumanBodyBones.RightUpperLeg);
                SetTransformField("rightKnee", HumanBodyBones.RightLowerLeg);
                SetTransformField("rightFoot", HumanBodyBones.RightFoot);
                SetTransformField("leftArm", HumanBodyBones.LeftUpperArm);
                SetTransformField("leftElbow", HumanBodyBones.LeftLowerArm);
                SetTransformField("rightArm", HumanBodyBones.RightUpperArm);
                SetTransformField("rightElbow", HumanBodyBones.RightLowerArm);
                SetTransformField("middleSpine", HumanBodyBones.Spine);
                SetTransformField("head", HumanBodyBones.Head);

                MethodInfo method = RagdollBuilderType.GetMethod("OnWizardUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(RagDollWizardWindow, null);
                }
            }
            else {
                Debug.LogWarning("Assigning process is only support humanoid type animator.");
            }
        }
        [MenuItem("CONTEXT/Animator/Remove CharacterJoints And Rigidbodies")]
        static void RemoveCharacterJointsAndRigidbodies()
        {
            Undo.SetCurrentGroupName("Remove CharacterJoints And Rigidbodies");
            int group = Undo.GetCurrentGroup();
            var target = Selection.activeGameObject;
            foreach (var item in target.GetComponentsInChildren<CharacterJoint>())
            {
                var rBody = item.connectedBody;
                if (item != null)
                    Undo.DestroyObjectImmediate(item);
                if (rBody != null)
                    Undo.DestroyObjectImmediate(rBody);
            }
            var rigibodies = target.GetComponentsInChildren<Rigidbody>();
            foreach (var item in rigibodies)
            {
                if (item != null)
                    Undo.DestroyObjectImmediate(item);
            }
            Undo.CollapseUndoOperations(group);
        }

 
        //[MenuItem("CONTEXT/Animator/Create Skeleton Collider")]
        //public static void CreateSkeletonCollider(MenuCommand command) {
        //    Animator animator = (Animator)command.context;
        //    Undo.SetCurrentGroupName("Create Skeleton Collider");
        //    int group = Undo.GetCurrentGroup();
        //    var HumanBodyBonesDatas = AnimatorEx.GetHumanBodyBonesData(animator);


        //    var head = HumanBodyBonesDatas[HumanBodyBones.Head];            
        //    var spcol = head.UndoGetOrAddComponet<SphereCollider>();
        //    spcol.radius = 0.125f;
        //    spcol.center = head.localPosition.normalized * spcol.radius ;
        //    var neck = HumanBodyBonesDatas[HumanBodyBones.Neck];

        //    var cc =  neck.UndoGetOrAddComponet<CapsuleCollider>();

        //    cc.radius = 0.05f;
        //    cc.direction = GetCapsuleColliderDirection(head.localPosition);
        //    //cc.height = Vector3.Distance(head.position, neck.position);
        //    //cc.center = head.localPosition.normalized*cc.radius;
        //    cc.center = new Vector3(-0.05f, 0, 0);



        //    //spcol.center = new Vector3();

        //    //foreach (var t in )
        //    //{
        //    //    if (t != null)
        //    //    {

        //    //        collider.radius = 0.1f;
        //    //        collider.direction = 0;
        //    //        if (t.childCount > 0) {
        //    //            collider.height = Vector3.Distance(t.position, t.GetChild(0).position);
        //    //        }
        //    //        collider.center = new Vector3(-collider.height * 0.5f, 0, 0);
        //    //    }
        //    //}
        //    Undo.CollapseUndoOperations(group);
        //}
    }
}
