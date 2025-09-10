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
//        // 當編輯器載入時，註冊自定 Build 處理器
//        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildButtonPressed);
//    }

//    static void OnBuildButtonPressed(BuildPlayerOptions options)
//    {
        
//        //// 儲存原始值
//        //originalJobWorkerCount = JobsUtility.JobWorkerCount;

//        //// 立即設定成你要的 JobWorkerCount（這裡設為 2）
//        //JobsUtility.JobWorkerCount = 2;
//        //Debug.Log($"[JobWorkerCountBuildHook] Build 開始：設定 JobWorkerCount = 2");

//        //// 執行 Build
//        //var report = BuildPipeline.BuildPlayer(options);

//        //// Build 完成後恢復原值
//        //JobsUtility.JobWorkerCount = originalJobWorkerCount;
//        //Debug.Log($"[JobWorkerCountBuildHook] Build 結束：還原 JobWorkerCount = {originalJobWorkerCount}");

//        //// 顯示 Build 結果摘要
//        //Debug.Log($"[JobWorkerCountBuildHook] Build 結果：{report.summary.result}, 平台：{report.summary.platform}, 檔案大小：{report.summary.totalSize / (1024f * 1024f):F2} MB");
//    }
//}