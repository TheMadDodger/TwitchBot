using System;
using System.Media;
using System.IO;
using System.Collections.Generic;

namespace BotOfSparta
{
    #region Helper Structs

    struct AudioCost
    {
        public uint RequiredItemID;
        public uint CostItemID;
        public uint CostAmount;
    }

    struct AudioRequestResult
    {
        public bool Succes;
        public bool InvalidFile;
        public bool GlobalCooldown;
        public float Cooldown;
        public string Message;
        public AudioCost Cost;
    }

    #endregion

    class AudioData
    {
        #region Fields

        private SoundPlayer m_Player;
        private DateTime m_LastPlayed;
        public float m_Cooldown;
        public string m_Message;
        public AudioCost Cost;

        #endregion

        #region Initialization

        public AudioData(string path, float coolDown, string message)
        {
            // getting root path
            string rootLocation = typeof(Program).Assembly.Location;
            rootLocation = rootLocation.Remove(rootLocation.Length - 4 - "SpartanTwitchBotRemastered".Length);
            // appending sound location
            string fullPathToSound = Path.Combine(rootLocation, path);
            m_Player = new SoundPlayer(fullPathToSound);
            DateTime m_LastPlayed = DateTime.Now;
            m_Cooldown = coolDown;
            m_Message = message;
        }

        #endregion

        #region Methods

        public bool Play(bool byPassCooldown = false)
        {
            if(!byPassCooldown)
            {
                if (CheckCooldown()) return false;
            }
            if (!File.Exists(m_Player.SoundLocation)) return false;
            m_Player.Play();
            m_LastPlayed = DateTime.Now;
            return true;
        }

        public bool CheckCooldown()
        {
            DateTime current = DateTime.Now;
            TimeSpan difference = current.Subtract(m_LastPlayed);
            return !(difference.TotalSeconds >= m_Cooldown);
        }

        public bool CanPlay(bool byPassCooldown = false)
        {
            if (!byPassCooldown)
            {
                if (CheckCooldown()) return false;
            }
            if (!File.Exists(m_Player.SoundLocation)) return false;
            return true;
        }

        #endregion
    }

    class SoundFX
    {
        #region Fields

        private Dictionary<string, AudioData> m_FXMap = new Dictionary<string, AudioData>();
        private int m_EffectSpam = 0; // Used to determine global cooldown
        private readonly int m_MaxEffectSpam = 5;
        private DateTime m_TimeSinceLastRequest = DateTime.Now;
        private float m_MinTimeBetweenRequests = 30.0f;

        #endregion

        #region Initialization

        public SoundFX()
        {
            AddSoundFX("biscuits", @"Resources\SoundFX\snd_biscuits.wav", 30.0f, "Well bite my bisquits!", 3, 100);
            AddSoundFX("coalplant", @"Resources\SoundFX\snd_coalplant.wav", 30.0f, "", 3, 200);
            AddSoundFX("ezgame", @"Resources\SoundFX\snd_ezgame.wav", 30.0f, "EZ game EZ life!", 3, 350);
            AddSoundFX("frisky", @"Resources\SoundFX\snd_frisky.wav", 30.0f, "Lets get frisky!", 3, 100);
            AddSoundFX("ufuckeditup", @"Resources\SoundFX\snd_fucked_up.wav", 30.0f, "U fucked it up!", 2, 100);
            AddSoundFX("gotcha", @"Resources\SoundFX\snd_gotcha_bitch.wav", 30.0f, "Gotcha bitch!", 3, 200);
            AddSoundFX("justdoitdb", @"Resources\SoundFX\snd_justdoitdb.wav", 30.0f, "JUST... DO IT!", 3, 150);
            AddSoundFX("punch", @"Resources\SoundFX\snd_punch.wav", 30.0f, "Punch in da face!", 3, 50);
            AddSoundFX("sadfail", @"Resources\SoundFX\snd_sad_fail.wav", 30.0f, ":(", 2, 250);
            AddSoundFX("surprise", @"Resources\SoundFX\snd_surprise.wav", 30.0f, "Surprise mudafuka!", 2, 1);
            AddSoundFX("udone", @"Resources\SoundFX\snd_udone.wav", 30.0f, "Are you done?", 2, 100);
            AddSoundFX("weed", @"Resources\SoundFX\snd_weed.wav", 30.0f, "Smoke weed everyday!", 1, 1);
            AddSoundFX("wizzard", @"Resources\SoundFX\snd_wizzard.wav", 30.0f, "Harry, you're a wizzard!", 2, 250);
            AddSoundFX("onejob", @"Resources\SoundFX\snd_onejob.wav", 30.0f, "You had one job...", 3, 500);
            AddSoundFX("lj", @"Resources\SoundFX\snd_leeroy.wav", 30.0f, "LEEEEEEROOOOOY... JENKIIIIIIIINS!", 2, 300);
            AddSoundFX("sparta", @"Resources\SoundFX\snd_thisissparta.wav", 30.0f, "THIS... IS... SPARTAAAAAAAAAA!", 2, 300);

            AddSoundFX("sacredsparta", @"Resources\SoundFX\snd_scaredSparta.wav", 30.0f, "https://clips.twitch.tv/EnticingYawningCheesecakeFrankerZ", 3, 150);
            AddSoundFX("fuhzecret", @"Resources\SoundFX\snd_FUHZecret.wav", 30.0f, "This must be some kind off... FUHZECRET!", 3, 150);
        }

        #endregion

        #region Methods

        public void AddSoundFX(string name, string path, float coolDown, string message, uint itemID, uint itemCost)
        {
            var data = new AudioData(path, coolDown, message);
            data.Cost.CostItemID = itemID;
            data.Cost.CostAmount = itemCost;
            m_FXMap.Add(name, data);
        }

        public bool CheckSpam()
        {
            DateTime current = DateTime.Now;
            TimeSpan difference = current.Subtract(m_TimeSinceLastRequest);

            if(difference.TotalSeconds >= m_MinTimeBetweenRequests)
            {
                m_EffectSpam = 0;
            }

            return !(m_EffectSpam <= m_MaxEffectSpam);
        }

        public AudioRequestResult CanPlay(string name, bool byPassCooldown, bool byPassAntiSpam = false)
        {
            AudioRequestResult result = new AudioRequestResult();
            if (!byPassAntiSpam)
            {
                if (CheckSpam())
                {
                    result.Succes = false;
                    result.InvalidFile = false;
                    result.GlobalCooldown = true;
                    return result;
                }
            }

            AudioData data;
            if (m_FXMap.TryGetValue(name, out data))
            {
                if (data.CanPlay(byPassCooldown))
                {
                    result.Succes = true;
                    result.InvalidFile = false;
                    result.Message = data.m_Message;
                    result.Cost = data.Cost;
                    return result;
                }
                else
                {
                    result.Succes = false;
                    result.InvalidFile = false;
                    result.Cooldown = data.m_Cooldown;
                    return result;
                }
            }
            else
            {
                result.Succes = false;
                result.InvalidFile = true;
                return result;
            }
        }

        public AudioRequestResult Play(string name, bool byPassCooldown, bool byPassAntiSpam = false)
        {
            AudioRequestResult result = new AudioRequestResult();
            if (!byPassAntiSpam)
            {
                if (CheckSpam())
                {
                    result.Succes = false;
                    result.InvalidFile = false;
                    result.GlobalCooldown = true;
                    return result;
                }
            }

            AudioData data;
            if (m_FXMap.TryGetValue(name, out data))
            {
                if (data.Play(byPassCooldown))
                {
                    m_TimeSinceLastRequest = DateTime.Now;
                    result.Succes = true;
                    result.InvalidFile = false;
                    result.Message = data.m_Message;
                    result.Cost = data.Cost;
                    ++m_EffectSpam;
                    return result;
                }
                else
                {
                    result.Succes = false;
                    result.InvalidFile = false;
                    result.Cooldown = data.m_Cooldown;
                    return result;
                }
            }
            else
            {
                result.Succes = false;
                result.InvalidFile = true;
                return result;
            }
        }

        #endregion
    }
}
