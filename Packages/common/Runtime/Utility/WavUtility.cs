using System;
using UnityEngine;
using Yu5h1Lib;

public static class WavUtility 
{
    public static bool TryCreateAudioClip(byte[] wavFile, out AudioClip clip, int offsetSamples = 0, string name = "wav")
    {
        clip = null;

        if (wavFile.IsEmpty() || wavFile.Length < 44) // minimum WAV header size
            return false;

        try
        {
            int channels = BitConverter.ToInt16(wavFile, 22);
            int sampleRate = BitConverter.ToInt32(wavFile, 24);
            int subchunk2 = FindSubchunk(wavFile, "data", 36);
            if (subchunk2 <= 0 || subchunk2 >= wavFile.Length)
                return false;

            int samples = (wavFile.Length - subchunk2) / 2;
            float[] data = new float[samples];
            int i = 0;

            for (int j = subchunk2; j < wavFile.Length; j += 2)
            {
                short sample = BitConverter.ToInt16(wavFile, j);
                data[i++] = sample / 32768.0f;
            }

            clip = AudioClip.Create(name, samples, channels, sampleRate, false);
            clip.SetData(data, offsetSamples);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int FindSubchunk(byte[] bytes, string id, int startIndex)
    {
        while (startIndex < bytes.Length - 8)
        {
            string chunkID = System.Text.Encoding.ASCII.GetString(bytes, startIndex, 4);
            int chunkSize = BitConverter.ToInt32(bytes, startIndex + 4);

            if (chunkID == id)
                return startIndex + 8;

            startIndex += 8 + chunkSize;
        }

        return -1; //not found
    }
}
