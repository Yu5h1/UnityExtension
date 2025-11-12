using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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
    public event Action<float[]> OnAudioDataAvailable
    {
        add => _audioDataAvailable.AddListener(new UnityAction<float[]>(value));
        remove => _audioDataAvailable.RemoveListener(new UnityAction<float[]>(value));
    }

    public event Action<float> OnRMSChanged
    {
        add => _rmsChanged.AddListener(new UnityAction<float>(value));
        remove => _rmsChanged.RemoveListener(new UnityAction<float>(value));
    }

    public event Action OnRecordingStarted
    {
        add => _recordingStarted.AddListener(new UnityAction(value));
        remove => _recordingStarted.RemoveListener(new UnityAction(value));
    }

    public event Action OnRecordingStopped
    {
        add => _recordingStopped.AddListener(new UnityAction(value));
        remove => _recordingStopped.RemoveListener(new UnityAction(value));
    }

    public event Action OnSpeechStarted
    {
        add => _speechStarted.AddListener(new UnityAction(value));
        remove => _speechStarted.RemoveListener(new UnityAction(value));
    }

    public event Action OnSpeechStopped
    {
        add => _speechStopped.AddListener(new UnityAction(value));
        remove => _speechStopped.RemoveListener(new UnityAction(value));
    }

    public event Action<string> OnError
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
    protected bool isSpeaking = false;
    protected int silenceFrameCount = 0;
    protected Coroutine captureCoroutine;

    // 公開屬性
    public bool IsRecording => isRecording;
    public bool IsSpeaking => isSpeaking;
    public float CurrentRMS { get; protected set; }
    public float SmoothedRMS { get; protected set; }
    public int SampleRate => sampleRate;
    public string SelectedDevice => selectedMicrophone;

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

        if (Microphone.devices.Length == 0)
        {
            string error = "No microphone found";
            Debug.LogError($"❌ {error}");
            _error?.Invoke(error);
            return;
        }

        selectedMicrophone = Microphone.devices[0];
        Debug.Log($"🎤 Starting microphone: {selectedMicrophone}");

        microphoneClip = Microphone.Start(selectedMicrophone, true, 10, sampleRate);

        if (microphoneClip == null)
        {
            string error = "Failed to create microphone clip";
            Debug.LogError($"❌ {error}");
            _error?.Invoke(error);
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

        isRecording = false;
        isSpeaking = false;

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
        silenceFrameCount = 0;

        _recordingStopped?.Invoke();
    }

    protected virtual IEnumerator InitializeAndCapture()
    {
        int attempts = 0;
        int maxAttempts = 300;

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
            _error?.Invoke(error);
            yield break;
        }

        isRecording = true;
        _recordingStarted?.Invoke();

        captureCoroutine = StartCoroutine(CaptureAudio());
    }

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

                float rms = CalculateRMS(buffer);
                UpdateRMS(rms);

                float[] bufferCopy = new float[buffer.Length];
                Array.Copy(buffer, bufferCopy, buffer.Length);
                _audioDataAvailable?.Invoke(bufferCopy);

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

        _rmsChanged?.Invoke(CurrentRMS);

        DetectVoiceActivity(CurrentRMS);
    }

    protected virtual void DetectVoiceActivity(float rms)
    {
        if (!isSpeaking)
        {
            if (rms > speechThreshold)
            {
                isSpeaking = true;
                silenceFrameCount = 0;
                _speechStarted?.Invoke();
                Debug.Log("🗣️ Speech started");
            }
        }
        else
        {
            if (rms < silenceThreshold)
            {
                silenceFrameCount++;

                if (silenceFrameCount >= silenceFramesRequired)
                {
                    isSpeaking = false;
                    silenceFrameCount = 0;
                    _speechStopped?.Invoke();
                    Debug.Log("🤐 Speech stopped");
                }
            }
            else
            {
                silenceFrameCount = 0;
            }
        }
    }

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