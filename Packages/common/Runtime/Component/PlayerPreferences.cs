using UnityEngine;

namespace Yu5h1Lib
{
	public class PlayerPreferences : Preferences<PlayerPreferences>
    {
        [ContextMenu(nameof(PrintPlayerPrefs))]
        public void PrintPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(KEY))
                $"PlayerPrefs[{KEY}] = {PlayerPrefs.GetString(KEY)}".print();
            else
                $"PlayerPrefs does not contain key [{KEY}]".printWarning();
        }
        [ContextMenu(nameof(ClearPlayerPrefs))]
        public void ClearPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(KEY))
            {
                PlayerPrefs.DeleteKey(KEY);
                $"Cleared PlayerPrefs for key [{KEY}]".print();
            }
            else
                $"PlayerPrefs does not contain key [{KEY}]".printWarning();

        }

    
    }
}