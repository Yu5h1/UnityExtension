using UnityEditor;
using UnityEngine;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

//[InitializeOnLoad]
//public class JobWorkerCountBuildHook
//{
//    static int originalJobWorkerCount;

//    static JobWorkerCountBuildHook()
//    {
//        // ��s�边���J�ɡA���U�۩w Build �B�z��
//        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildButtonPressed);
//    }

//    static void OnBuildButtonPressed(BuildPlayerOptions options)
//    {
        
//        //// �x�s��l��
//        //originalJobWorkerCount = JobsUtility.JobWorkerCount;

//        //// �ߧY�]�w���A�n�� JobWorkerCount�]�o�̳]�� 2�^
//        //JobsUtility.JobWorkerCount = 2;
//        //Debug.Log($"[JobWorkerCountBuildHook] Build �}�l�G�]�w JobWorkerCount = 2");

//        //// ���� Build
//        //var report = BuildPipeline.BuildPlayer(options);

//        //// Build �������_���
//        //JobsUtility.JobWorkerCount = originalJobWorkerCount;
//        //Debug.Log($"[JobWorkerCountBuildHook] Build �����G�٭� JobWorkerCount = {originalJobWorkerCount}");

//        //// ��� Build ���G�K�n
//        //Debug.Log($"[JobWorkerCountBuildHook] Build ���G�G{report.summary.result}, ���x�G{report.summary.platform}, �ɮפj�p�G{report.summary.totalSize / (1024f * 1024f):F2} MB");
//    }
//}