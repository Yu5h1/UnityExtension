using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 麥克風控制器基礎版本 - 不依賴任何第三方套件
/// </summary>
public class MicrophoneController : MonoBehaviour
{
    [Header("Microphone Settings")]
    [SerializeField] protected int sampleRate = 16000;
    [SerializeField] protected int chunkSize = 512;
    [SerializeField] protected float rmsAlpha = 0.15f;
    [SerializeField] protected float decayFactor = 0.92f;

    [SerializeField] private UnityEvent<float[]> _AudioDataAvailable;
    public event Action<float[]> AudioDataAvailable
    { 
        add => _AudioDataAvailable.AddListener(new UnityAction<float[]>(value)); 
        remove => _AudioDataAvailable.RemoveListener(new UnityAction<float[]>(value)); 
    }
    [SerializeField] private UnityEvent<float> _RMSChanged;
    public event Action<float> RMSChanged
    { 
        add => _RMSChanged.AddListener(new UnityAction<float>(value)); 
        remove => _RMSChanged.RemoveListener(new UnityAction<float>(value));
    }
    [SerializeField] private UnityEvent _RecordingStarted;
    public event Action RecordingStarted
    { 
        add => _RecordingStarted.AddListener(new UnityAction(value)); 
        remove => _RecordingStarted.RemoveListener(new UnityAction(value));
    }
    [SerializeField] private UnityEvent _RecordingStopped;
    public event Action RecordingStopped
    { 
        add => _RecordingStopped.AddListener(new UnityAction(value)); 
        remove => _RecordingStopped.RemoveListener(new UnityAction(value));
    }
    [SerializeField] private UnityEvent<string> _ErrorOccurred;
    public event Action<string> ErrorOccurred
    { 
        add => _ErrorOccurred.AddListener(new UnityAction<string>(value)); 
        remove => _ErrorOccurred.RemoveListener(new UnityAction<string>(value));
    }

    // 狀態
    protected AudioClip microphoneClip;
    protected string selectedMicrophone;
    protected int micPosition = 0;
    protected int lastMicPosition = 0;
    protected bool isRecording = false;
    protected Coroutine captureCoroutine;

    // 公開屬性
    public bool IsRecording => isRecording;
    public float CurrentRMS { get; protected set; }
    public float SmoothedRMS { get; protected set; }
    public int SampleRate => sampleRate;
    public string SelectedDevice => selectedMicrophone;

    /// <summary>
    /// 開始錄音（同步版本，使用 Coroutine）
    /// </summary>
    public virtual void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("⚠️ Microphone already recording");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            string error = "No microphone found";
            Debug.LogError($"❌ {error}");
            _ErrorOccurred?.Invoke(error);
            return;
        }

        selectedMicrophone = Microphone.devices[0];
        Debug.Log($"🎤 Starting microphone: {selectedMicrophone}");

        microphoneClip = Microphone.Start(selectedMicrophone, true, 10, sampleRate);

        if (microphoneClip == null)
        {
            string error = "Failed to create microphone clip";
            Debug.LogError($"❌ {error}");
            _ErrorOccurred?.Invoke(error);
            return;
        }

        // 啟動初始化和捕獲協程
        StartCoroutine(InitializeAndCapture());
    }

    /// <summary>
    /// 停止錄音
    /// </summary>
    public virtual void StopRecording()
    {
        if (!isRecording)
            return;

        isRecording = false;

        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        if (!string.IsNullOrEmpty(selectedMicrophone))
        {
            Microphone.End(selectedMicrophone);
            Debug.Log("🛑 Microphone stopped");
        }

        microphoneClip = null;
        CurrentRMS = 0f;
        SmoothedRMS = 0f;

        _RecordingStopped?.Invoke();
    }

    /// <summary>
    /// 初始化並開始捕獲
    /// </summary>
    protected virtual IEnumerator InitializeAndCapture()
    {
        // 等待麥克風初始化
        int attempts = 0;
        int maxAttempts = 300; // 30 秒

        while (attempts < maxAttempts)
        {
            int position = Microphone.GetPosition(selectedMicrophone);

            if (position > 0)
            {
                Debug.Log($"✅ Microphone ready at position: {position}");
                break;
            }

            attempts++;

            if (attempts % 10 == 0)
            {
                Debug.Log($"⏳ Waiting for microphone... ({attempts * 0.1f:F1}s)");
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (attempts >= maxAttempts)
        {
            string error = "Microphone initialization timeout";
            Debug.LogError($"❌ {error}");
            _ErrorOccurred?.Invoke(error);
            yield break;
        }

        // 標記為正在錄音
        isRecording = true;
        _RecordingStarted?.Invoke();

        // 開始捕獲音頻
        captureCoroutine = StartCoroutine(CaptureAudio());
    }

    /// <summary>
    /// 捕獲音頻數據
    /// </summary>
    protected virtual IEnumerator CaptureAudio()
    {
        float[] buffer = new float[chunkSize];

        while (isRecording)
        {
            micPosition = Microphone.GetPosition(selectedMicrophone);

            if (micPosition < 0 || micPosition == lastMicPosition)
            {
                yield return null;
                continue;
            }

            int diff = micPosition - lastMicPosition;
            if (diff < 0)
                diff += microphoneClip.samples;

            if (diff >= chunkSize)
            {
                microphoneClip.GetData(buffer, lastMicPosition);

                // 計算 RMS
                float rms = CalculateRMS(buffer);
                UpdateRMS(rms);

                // 觸發事件（複製 buffer 避免被修改）
                float[] bufferCopy = new float[buffer.Length];
                Array.Copy(buffer, bufferCopy, buffer.Length);
                _AudioDataAvailable?.Invoke(bufferCopy);

                lastMicPosition = (lastMicPosition + chunkSize) % microphoneClip.samples;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 計算 RMS
    /// </summary>
    protected float CalculateRMS(float[] buffer)
    {
        float sum = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            sum += buffer[i] * buffer[i];
        }
        return Mathf.Sqrt(sum / buffer.Length);
    }

    /// <summary>
    /// 更新 RMS（平滑處理）
    /// </summary>
    protected virtual void UpdateRMS(float newRMS)
    {
        CurrentRMS = (1f - rmsAlpha) * CurrentRMS + rmsAlpha * newRMS;
        SmoothedRMS = Mathf.Max(SmoothedRMS, CurrentRMS);

        _RMSChanged?.Invoke(CurrentRMS);
    }

    /// <summary>
    /// Update 中衰減 RMS
    /// </summary>
    protected virtual void Update()
    {
        if (isRecording)
        {
            SmoothedRMS *= decayFactor;
            CurrentRMS *= decayFactor;
        }
    }

    protected virtual void OnDestroy()
    {
        StopRecording();
    }

    protected virtual void OnApplicationQuit()
    {
        StopRecording();
    }
}