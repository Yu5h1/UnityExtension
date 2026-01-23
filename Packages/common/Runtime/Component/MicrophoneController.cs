using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

/// <summary>
/// 麥克風控制器基礎版本 - 使用 UnityEvent
/// </summary>
public class MicrophoneController : MonoBehaviour
{
    
    [Header("Microphone Settings")]
    [SerializeField] protected int sampleRate = 16000;
    [SerializeField] protected int chunkSize = 512;
    [SerializeField] protected float rmsAlpha = 0.15f;
    [SerializeField] protected float decayFactor = 0.92f;

    [Header("Voice Activity Detection")]
    [SerializeField] protected float speechThreshold = 0.02f;
    [SerializeField] protected float silenceThreshold = 0.01f;
    [SerializeField] protected int silenceFramesRequired = 10;

    [Header("Events")]
    [SerializeField] protected UnityEvent<float[]> _audioDataAvailable;
    [SerializeField] protected UnityEvent<float> _rmsChanged;
    [SerializeField] protected UnityEvent _recordingStarted;
    [SerializeField] protected UnityEvent _recordingStopped;
    [SerializeField] protected UnityEvent _speechStarted;

    [ContextMenu(nameof(Test_speechStarted))]
    public void Test_speechStarted() => _speechStarted?.Invoke();

    [SerializeField] protected UnityEvent _speechStopped;
    [SerializeField] protected UnityEvent<string> _error;

    // C# 事件包裝器
    public event Action<float[]> audioDataAvailable
    {
        add => _audioDataAvailable.AddListener(new UnityAction<float[]>(value));
        remove => _audioDataAvailable.RemoveListener(new UnityAction<float[]>(value));
    }

    public event Action<float> RMSChanged
    {
        add => _rmsChanged.AddListener(new UnityAction<float>(value));
        remove => _rmsChanged.RemoveListener(new UnityAction<float>(value));
    }

    public event Action recordingStarted
    {
        add => _recordingStarted.AddListener(new UnityAction(value));
        remove => _recordingStarted.RemoveListener(new UnityAction(value));
    }

    public event Action recordingStopped
    {
        add => _recordingStopped.AddListener(new UnityAction(value));
        remove => _recordingStopped.RemoveListener(new UnityAction(value));
    }

    public event Action speechStarted
    {
        add => _speechStarted.AddListener(new UnityAction(value));
        remove => _speechStarted.RemoveListener(new UnityAction(value));
    }

    public event Action speechStopped
    {
        add => _speechStopped.AddListener(new UnityAction(value));
        remove => _speechStopped.RemoveListener(new UnityAction(value));
    }

    public event Action<string> Errored
    {
        add => _error.AddListener(new UnityAction<string>(value));
        remove => _error.RemoveListener(new UnityAction<string>(value));
    }

    // 狀態
    protected AudioClip microphoneClip;
    protected string selectedMicrophone;
    protected int micPosition = 0;
    protected int lastMicPosition = 0;
    protected bool isRecording = false;
    protected int silenceFrameCount = 0;
    protected Coroutine captureCoroutine;

    // Properties
    public bool IsRecording => isRecording;
    public bool IsSpeaking { get; protected set; } = false;
    private float speechStartTime;
    public float SpeechDuration => IsSpeaking ? Time.realtimeSinceStartup - speechStartTime : 0f;
    public float CurrentRMS { get; protected set; }
    public float SmoothedRMS { get; protected set; }
    public int SampleRate => sampleRate;
    public string SelectedDevice => selectedMicrophone;

    public string[] devices
    {
        get
        {
#if USE_MICROPHONE
            return Microphone.devices;
#else
            return Array.Empty<string>();
#endif
        }
    }

    #region Microphone Platform Abstraction

    public AudioClip StartMicrophone(string deviceName, bool loop, int lengthSec, int frequency)
    {
#if USE_MICROPHONE
        return Microphone.Start(deviceName, loop, lengthSec, frequency);
#else
        return null;
#endif
    }

    public void EndMicrophone(string deviceName)
    {
#if USE_MICROPHONE
        Microphone.End(deviceName);
#endif
    }

    public int GetPosition(string deviceName)
    {
#if USE_MICROPHONE
        return Microphone.GetPosition(deviceName);
#else
        return -1;
#endif
    }

    #endregion

    #region Event Triggers (統一事件觸發點)

    /// <summary>
    /// 錄音開始時呼叫
    /// </summary>
    protected virtual void OnRecordingStarted()
    {
        isRecording = true;
        lastMicPosition = 0;
        micPosition = 0;
        Debug.Log("🎤 Recording started");
        _recordingStarted?.Invoke();
    }

    /// <summary>
    /// 錄音停止時呼叫
    /// </summary>
    protected virtual void OnRecordingStopped()
    {
        isRecording = false;
        IsSpeaking = false;
        CurrentRMS = 0f;
        SmoothedRMS = 0f;
        silenceFrameCount = 0;
        microphoneClip = null;
        Debug.Log("🛑 Recording stopped");
        _recordingStopped?.Invoke();
    }

    /// <summary>
    /// 偵測到語音開始時呼叫
    /// </summary>
    protected virtual void OnSpeechStarted()
    {
        speechStartTime = Time.realtimeSinceStartup;
        IsSpeaking = true;
        silenceFrameCount = 0;
        Debug.Log("🗣️ Speech started");
        _speechStarted?.Invoke();
    }

    /// <summary>
    /// 偵測到語音結束時呼叫
    /// </summary>
    protected virtual void OnSpeechStopped()
    {
        IsSpeaking = false;
        silenceFrameCount = 0;
        Debug.Log("🤐 Speech stopped");
        _speechStopped?.Invoke();
    }

    /// <summary>
    /// 收到音訊資料時呼叫
    /// </summary>
    protected virtual void OnAudioDataAvailable(float[] buffer)
    {
        _audioDataAvailable?.Invoke(buffer);
    }

    /// <summary>
    /// RMS 值更新時呼叫
    /// </summary>
    protected virtual void OnRMSChanged(float rms)
    {
        _rmsChanged?.Invoke(rms);
    }

    /// <summary>
    /// 發生錯誤時呼叫
    /// </summary>
    protected virtual void OnError(string error)
    {
        Debug.LogError($"❌ {error}");
        _error?.Invoke(error);
    }

    #endregion

    #region Recording Control

    /// <summary>
    /// 開始錄音
    /// </summary>
    public virtual void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("⚠️ Microphone already recording");
            return;
        }

        if (devices.Length == 0)
        {
            OnError("No microphone found");
            return;
        }

        selectedMicrophone = devices[0];
        Debug.Log($"🎤 Starting microphone: {selectedMicrophone}");

        microphoneClip = StartMicrophone(selectedMicrophone, true, 10, sampleRate);

        if (microphoneClip == null)
        {
            OnError("Failed to create microphone clip");
            return;
        }

        StartCoroutine(InitializeAndCapture());
    }

    /// <summary>
    /// 停止錄音
    /// </summary>
    public virtual void StopRecording()
    {
        if (!isRecording)
            return;

        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        if (!selectedMicrophone.IsEmpty())
        {
            EndMicrophone(selectedMicrophone);
        }

        OnRecordingStopped();
    }

    #endregion

    #region Audio Processing

    protected virtual IEnumerator InitializeAndCapture()
    {
        int attempts = 0;
        int maxAttempts = 300;

        while (attempts < maxAttempts)
        {
            int position = GetPosition(selectedMicrophone);

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
            OnError("Microphone initialization timeout");
            yield break;
        }

        OnRecordingStarted();
        captureCoroutine = StartCoroutine(CaptureAudio());
    }

    protected virtual IEnumerator CaptureAudio()
    {
        float[] buffer = new float[chunkSize];

        while (isRecording)
        {
            micPosition = GetPosition(selectedMicrophone);

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

                float rms = CalculateRMS(buffer);
                UpdateRMS(rms);

                float[] bufferCopy = new float[buffer.Length];
                Array.Copy(buffer, bufferCopy, buffer.Length);
                OnAudioDataAvailable(bufferCopy);

                lastMicPosition = (lastMicPosition + chunkSize) % microphoneClip.samples;
            }

            yield return null;
        }
    }

    protected float CalculateRMS(float[] buffer)
    {
        float sum = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            sum += buffer[i] * buffer[i];
        }
        return Mathf.Sqrt(sum / buffer.Length);
    }

    protected virtual void UpdateRMS(float newRMS)
    {
        CurrentRMS = (1f - rmsAlpha) * CurrentRMS + rmsAlpha * newRMS;
        SmoothedRMS = Mathf.Max(SmoothedRMS, CurrentRMS);

        OnRMSChanged(CurrentRMS);
        DetectVoiceActivity(CurrentRMS);
    }

    protected virtual void DetectVoiceActivity(float rms)
    {
        if (!IsSpeaking)
        {
            if (rms > speechThreshold)
            {
                OnSpeechStarted();
            }
        }
        else
        {
            if (rms < silenceThreshold)
            {
                silenceFrameCount++;

                if (silenceFrameCount >= silenceFramesRequired)
                {
                    OnSpeechStopped();
                }
            }
            else
            {
                silenceFrameCount = 0;
            }
        }
    }

    #endregion

    #region Unity Lifecycle

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

    #endregion
}