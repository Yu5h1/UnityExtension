using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yu5h1Lib;

public static class LineRendererEx
{
    public static float GetLengthNormalized(this LineRenderer lineRenderer,int index)
    {
        var totalLength = lineRenderer.GetLength();
        var pointLength = lineRenderer.GetLength(endIndex:index);

        if (pointLength <= 0f)
            throw new InvalidOperationException("Point length is zero or negative.");

        return totalLength / pointLength;
    }
    public static float GetLength(this LineRenderer lineRenderer , int startIndex = 0,int endIndex = -1)
    {
        
        if (lineRenderer == null)
            throw new ArgumentNullException(nameof(lineRenderer));
        var lastIndex = lineRenderer.positionCount - 1;
        endIndex = (endIndex < startIndex || endIndex > lastIndex) ? lastIndex : endIndex;
        if (endIndex < startIndex)
            return 0;

        var length = 0f;
        for (int i = startIndex; i < endIndex; i++)
            length += Vector3.Distance(lineRenderer.GetPosition(i) , lineRenderer.GetPosition(i + 1));
        return length;
    }

    public static Mesh BuildMesh(this LineRenderer lineRenderer, ref Mesh mesh)
    {
        int positionCount = lineRenderer.positionCount;
        if (positionCount < 2)
        {
            Debug.LogWarning("LineRenderer has too few points to bake a mesh.");
            return null;
        }

        // 獲取點的位置
        Vector3[] positions = new Vector3[positionCount];
        lineRenderer.GetPositions(positions);

        // 頂點、法線和三角形索引構建
        Vector3[] vertices = new Vector3[positionCount * 2];
        Vector3[] normals = new Vector3[positionCount * 2]; // 法線
        int[] triangles = new int[(positionCount - 1) * 6];

        float width = lineRenderer.startWidth; // 假設寬度是固定的

        for (int i = 0; i < positionCount; i++)
        {
            // 計算每個頂點的左右法線
            Vector3 dir = Vector3.zero;
            if (i > 0) dir += (positions[i] - positions[i - 1]).normalized;
            if (i < positionCount - 1) dir += (positions[i + 1] - positions[i]).normalized;

            // 垂直於線段的方向（左側/右側）
            dir = Vector3.Cross(dir.normalized, Vector3.forward).normalized * (width / 2);

            // 計算左側和右側頂點的位置
            vertices[i * 2] = positions[i] - dir; // 左側頂點
            vertices[i * 2 + 1] = positions[i] + dir; // 右側頂點

            // 計算對應頂點的法線方向
            normals[i * 2] = -dir.normalized; // 左側頂點法線
            normals[i * 2 + 1] = dir.normalized; // 右側頂點法線
        }

        // 建立三角形索引
        for (int i = 0; i < positionCount - 1; i++)
        {
            int baseIndex = i * 2;
            int triIndex = i * 6;

            triangles[triIndex] = baseIndex;
            triangles[triIndex + 1] = baseIndex + 2;
            triangles[triIndex + 2] = baseIndex + 1;

            triangles[triIndex + 3] = baseIndex + 1;
            triangles[triIndex + 4] = baseIndex + 2;
            triangles[triIndex + 5] = baseIndex + 3;
        }

        // 設定 Mesh 資料
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals; // 設定法線
        mesh.triangles = triangles;

        return mesh;
    }
    public static void SetGradient(this LineRenderer lineRenderer, Color start ,Color end )
    {
        
        var g = lineRenderer.colorGradient;

        //var firstColor = lineRenderer.colorGradient.colorKeys.IsEmpty() ?
        //                 defaultColor : lineRenderer.colorGradient.colorKeys.First().color;
        //var lastColor = lineRenderer.colorGradient.colorKeys.IsEmpty() ?
        //                defaultColor : lineRenderer.colorGradient.colorKeys.Last().color;
        //var start = startColorParam ?? firstColor;
        //var end = endColorParam ?? firstColor;

        var alphakeys = lineRenderer.colorGradient.alphaKeys;
        var colorkeys = lineRenderer.colorGradient.colorKeys;

        if (alphakeys.Length != 8)
            Array.Resize(ref alphakeys, 8); 
        if (colorkeys.Length != 8)
            Array.Resize(ref colorkeys, 8);

        var interval = 1.0f / 7.0f;
        var t = 0f;
        for (int i = 0; i < 8; i++)
        {
            alphakeys[i] = g.alphaKeys[0];
            colorkeys[i].time = alphakeys[i].time = t;
            alphakeys[i].alpha = Mathf.Lerp(start.a, end.a, t);
            colorkeys[i].color = Color.Lerp(start,end,t);
            t += interval;
        }
        g.SetKeys(colorkeys, alphakeys);
        lineRenderer.colorGradient = g;
    }
    /// <summary>
    /// set 8 keys for Gradient
    /// </summary>
    public static void PrepareGradient(this LineRenderer lineRenderer, Color? defaultColor = null)
    {
        var g = lineRenderer.colorGradient;

        var colorKeys = g.colorKeys;
        var alphaKeys = g.alphaKeys;

        if (alphaKeys.Length == 8 && colorKeys.Length == 8)
            return;

        var start = defaultColor ?? (colorKeys.IsEmpty() ? Color.white : colorKeys.First().color);
        var end = defaultColor ?? (colorKeys.IsEmpty() ? Color.white : colorKeys.Last().color);

        lineRenderer.SetGradient(start,end);
    }


    public static void CalculateSideLinesOld(this LineRenderer mainLine, LineRenderer leftLine, LineRenderer rightLine, float sideWidth)
    {
        int count = mainLine.positionCount;
        if (count < 2) return;

        Vector3[] mainPoints = new Vector3[count];
        mainLine.GetPositions(mainPoints);

        // 計算主線寬度
        float mainWidth = (mainLine.startWidth + mainLine.endWidth) * 0.5f;
        float offset = (mainWidth + sideWidth) * 0.5f; // 統一計算 offset

        List<Vector3> leftPoints = new List<Vector3>();
        List<Vector3> rightPoints = new List<Vector3>();

        Vector3 tangent;
        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p1 = mainPoints[i];
            Vector3 p2 = mainPoints[i + 1];

            tangent = (p2 - p1).normalized;

            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0).normalized;

            leftPoints.Add(p1 - normal * offset);
            rightPoints.Add(p1 + normal * offset);
        }

        tangent = (mainPoints[count - 1] - mainPoints[count - 2]).normalized;
        Vector3 lastNormal = new Vector3(-tangent.y, tangent.x, 0).normalized;

        leftPoints.Add(mainPoints[count - 1] - lastNormal * offset);
        rightPoints.Add(mainPoints[count - 1] + lastNormal * offset);

        leftLine.positionCount = leftPoints.Count;
        leftLine.SetPositions(leftPoints.ToArray());
        leftLine.startWidth = sideWidth;
        leftLine.endWidth = sideWidth;

        rightLine.positionCount = rightPoints.Count;
        rightLine.SetPositions(rightPoints.ToArray());
        rightLine.startWidth = sideWidth;
        rightLine.endWidth = sideWidth;
    }
    public static void CalculateSideLinesOnPointAddedOld(this LineRenderer mainLine,
    LineRenderer leftLine, LineRenderer rightLine,
    float sideWidth,
    ref List<Vector3> leftPoints, ref List<Vector3> rightPoints)
    {
        int count = mainLine.positionCount;

        if (count < 2) return;

        Vector3[] mainPoints = new Vector3[mainLine.positionCount];
        mainLine.GetPositions(mainPoints);

        float mainWidth = (mainLine.startWidth + mainLine.endWidth) * 0.5f;
        float offset = (mainWidth + sideWidth) * 0.5f;

        int lastIndex = count - 1;

        Vector3 p1 = mainPoints[lastIndex - 1];
        Vector3 p2 = mainPoints[lastIndex];

        Vector3 tangent = (p2 - p1).normalized;

        Vector3 normal = new Vector3(-tangent.y, tangent.x, 0).normalized;

        leftPoints.Add(p1 - normal * offset);
        rightPoints.Add(p1 + normal * offset);
 
        leftLine.positionCount = leftPoints.Count;
        leftLine.SetPositions(leftPoints.ToArray());
        leftLine.startWidth = sideWidth;
        leftLine.endWidth = sideWidth;

        rightLine.positionCount = rightPoints.Count;
        rightLine.SetPositions(rightPoints.ToArray());
        rightLine.startWidth = sideWidth;
        rightLine.endWidth = sideWidth;

    }

    public static void CalculateSideLinesOnPointAdded(this LineRenderer mainLine,
        LineRenderer leftLine, LineRenderer rightLine,
        float sideWidth,
        ref List<Vector3> leftPoints, ref List<Vector3> rightPoints)
    {
        int count = mainLine.positionCount;

        // 確保至少有2個頂點才能進行計算
        if (count < 2) return;

        // 取得新增的頂點
        Vector3[] mainPoints = new Vector3[mainLine.positionCount];
        mainLine.GetPositions(mainPoints);

        // 計算主線的寬度（假設是平均寬度）
        float mainWidth = (mainLine.startWidth + mainLine.endWidth) * 0.5f;
        float baseOffset = (mainWidth + sideWidth) * 0.5f;

        // 只針對新增的頂點進行處理
        int lastIndex = count - 1;  // 最後一個新增頂點的索引

        // 取出最後兩個點
        Vector3 p1 = mainPoints[lastIndex - 1];
        Vector3 p2 = mainPoints[lastIndex];

        // 計算切線向量
        Vector3 tangent = (p2 - p1).normalized;

        // 計算內外側的偏移量
        float leftOffset = baseOffset;
        float rightOffset = baseOffset;

        // 計算法向量
        Vector3 normal = new Vector3(-tangent.y, tangent.x, 0).normalized;

        // 計算新增頂點對應的左右側點
        leftPoints.Add(p2 - normal * leftOffset);
        rightPoints.Add(p2 + normal * rightOffset);

        // 設定 LineRenderer 的頂點數量與位置
        leftLine.positionCount = leftPoints.Count;
        leftLine.SetPositions(leftPoints.ToArray());
        leftLine.startWidth = sideWidth;
        leftLine.endWidth = sideWidth;

        rightLine.positionCount = rightPoints.Count;
        rightLine.SetPositions(rightPoints.ToArray());
        rightLine.startWidth = sideWidth;
        rightLine.endWidth = sideWidth;

        //RemoveClosePoints(leftPoints, 0.05f);

        //SmoothLine(rightPoints, 0.2f);

        //ApplyLineRenderer(leftLine, leftPoints, sideWidth);
        //ApplyLineRenderer(rightLine, rightPoints, sideWidth);

    }

    public static void CalculateSideLines(this LineRenderer mainLine, LineRenderer leftLine, LineRenderer rightLine, float sideWidth)
    {
        int count = mainLine.positionCount;
        if (count < 2) return;

        Vector3[] mainPoints = new Vector3[count];
        mainLine.GetPositions(mainPoints);

        // 計算主線的平均寬度
        float mainWidth = (mainLine.startWidth + mainLine.endWidth) * 0.5f;
        float baseOffset = (mainWidth + sideWidth) * 0.5f;

        List<Vector3> leftPoints = new List<Vector3>();
        List<Vector3> rightPoints = new List<Vector3>();

        Vector3 prevTangent = Vector3.zero;
        Vector3 prevNormal = Vector3.zero;

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p1 = mainPoints[i];
            Vector3 p2 = mainPoints[i + 1];

            // 計算切線向量
            Vector3 tangent = (p2 - p1).normalized;

            // 計算內外側角度影響 (避免轉角處內側壓縮)
            float angleFactor = 1.0f;
            if (i > 0)
            {
                angleFactor = Mathf.Clamp01(Vector3.Dot(prevTangent, tangent));
            }

            float leftOffset = baseOffset * (0.8f + 0.2f * angleFactor);  // 內側縮小
            float rightOffset = baseOffset * (1.2f - 0.2f * angleFactor); // 外側放大

            // 計算法向量並平滑處理
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0).normalized;
            if (i > 0)
            {
                normal = Vector3.Lerp(prevNormal, normal, 0.5f).normalized; // 平滑轉角
            }

            // 計算左右側的點
            leftPoints.Add(p1 - normal * leftOffset);
            rightPoints.Add(p1 + normal * rightOffset);

            // 記錄上一個向量
            prevTangent = tangent;
            prevNormal = normal;
        }

        // 最後一個點
        Vector3 lastTangent = (mainPoints[count - 1] - mainPoints[count - 2]).normalized;
        Vector3 lastNormal = new Vector3(-lastTangent.y, lastTangent.x, 0).normalized;
        leftPoints.Add(mainPoints[count - 1] - lastNormal * baseOffset);
        rightPoints.Add(mainPoints[count - 1] + lastNormal * baseOffset);

        // 移除內側過近的點
        RemoveClosePoints(leftPoints, 0.05f);

        // 平滑補點 (外側)
        SmoothLine(rightPoints, 0.2f);

        // 設定 LineRenderer
        ApplyLineRenderer(leftLine, leftPoints, sideWidth);
        ApplyLineRenderer(rightLine, rightPoints, sideWidth,true);
    }

    // 移除過近的內側點 (避免內側變形)
    private static void RemoveClosePoints(List<Vector3> points, float minDistance)
    {
        for (int i = points.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(points[i], points[i - 1]) < minDistance)
            {
                points.RemoveAt(i);
            }
        }
    }

    // 平滑補點 (適用於外側)
    private static void SmoothLine(List<Vector3> points, float maxDistance)
    {
        for (int i = 1; i < points.Count; i++)
        {
            if (Vector3.Distance(points[i], points[i - 1]) > maxDistance)
            {
                Vector3 newPoint = (points[i] + points[i - 1]) * 0.5f; // 插入中點
                points.Insert(i, newPoint);
                i++; // 跳過新插入的點
            }
        }
    }

    // 套用到 LineRenderer
    private static void ApplyLineRenderer(LineRenderer line, List<Vector3> points, float width,bool revers = false)
    {
        line.positionCount = points.Count;
        var array = points.ToArray();
        if (revers)
            Array.Reverse(array);
        line.SetPositions(array);
        line.startWidth = width;
        line.endWidth = width;
    }
}
