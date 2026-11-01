using System;
using System.Collections.Generic;

namespace WonderJump.Save
{
    /// <summary>
    /// Local save file schema (JsonUtility-compatible: public fields, no dictionaries).
    /// Bump <see cref="CurrentSchemaVersion"/> on any breaking change and add a migration step.
    /// In co-op each player keeps their own save; nothing here is shared over the network.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        /// <summary>CharacterId name, e.g. "Leon".</summary>
        public string selectedCharacter = "Leon";
        public int totalCoins;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public SettingsData settings = new SettingsData();
    }

    [Serializable]
    public sealed class LevelProgress
    {
        /// <summary>LevelId string, e.g. "W1_L1".</summary>
        public string levelId;
        public bool unlocked;
        public bool completed;
        public int stars;
        public float bestTimeSeconds;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        /// <summary>Crystal Joystick size multiplier.</summary>
        public float joystickScale = 1f;
        public bool joystickFloating = true;
    }

    /// <summary>Raw storage seam. Phase 6: atomic file write in Application.persistentDataPath.</summary>
    public interface ISaveStore
    {
        bool TryRead(out string json);
        void Write(string json);
    }

    public interface ISaveService
    {
        SaveData Current { get; }
        /// <summary>Loads and migrates; falls back to a fresh SaveData on missing or corrupt files.</summary>
        void Load();
        void Save();
        event Action<SaveData> Saved;
    }
}
