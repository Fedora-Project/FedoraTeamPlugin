using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.GameSource;
using BrokeProtocol.LiteDB;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using BrokeProtocol.Utility.Networking;
using ENet;
using FedoraTeamPlugin.Commands;
using Newtonsoft.Json;
using UnityEngine;

namespace FedoraTeamPlugin
{
    public class Core : Plugin
    {
        public static Core Instance { get; private set; }
        public string Pub = "&4serveur soutenue par fedoraPlugin";
        public List<ShPlayer> vanished = new List<ShPlayer>();

        public Core()
        {
            if(Instance == null)
            {
                Instance = new Core();
            }
            Info = new PluginInfo("FedoraTeamPlugin", "fed");
            if (!IsValideLicense())
            {
                Debug.Log("Your License for FedoraTeamPlugin Is expired, pls Put a new one in config.json");
                return;
            }
            if (!Directory.Exists("./FedoraCore/Base")) { Directory.CreateDirectory("./FedoraCore/Base"); Debug.Log("[ Fedora ] " + "Creating Folder waiting pls..."); }
        }

        public string PathConfig = "./FedoraCore/Base/Config.json";

        public ConfigInfo LoadConfig()
        {
            if (!File.Exists(PathConfig))
            {
                SetupConfig();
            }
            else
            {
                var json = File.ReadAllText(PathConfig);
                return JsonConvert.DeserializeObject<ConfigInfo>(json);
            }
            ConfigInfo data = new ConfigInfo();
            Debug.Log("\n\n [default config] \n [choosed] !");
            return data;
        }

        public void SaveConfig(ConfigInfo data)
        {
            using (StreamWriter Sw = File.CreateText(PathConfig))
            {
                Sw.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            Debug.Log("Save Fedora Config ...");

        }

        public void SetupConfig()
        {
            if (!File.Exists(PathConfig))
            {
                ConfigInfo data = new ConfigInfo();
                using (StreamWriter Sw = File.CreateText(PathConfig))
                {
                    Sw.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                Debug.Log("creating Fedora Config ...");
            }
        }


        public string PathDB = "./FedoraCore/Base/Database/";
        public string PatchBan = "./FedoraCore/Base/Banned.json";

        public string GetPlayerDataPath(string user)
        {
            return Path.Combine(PathDB, user) + ".json";
        }

        public DataInfo LoadData(ShPlayer player)
        {
            if (!File.Exists(GetPlayerDataPath(player.username)))
            {
                DataInfo data = new DataInfo();
                data.PlayerName = player.username;
                data.Password = player.svPlayer.connectData.passwordHash;
                if (!Directory.Exists(PathDB)) { Directory.CreateDirectory(PathDB); Debug.Log("[TERRA] " + "Creating Folder waiting pls..."); }
                if (!Directory.Exists(GetPlayerDataPath(data.PlayerName)))
                {
                    using (StreamWriter Sw = File.CreateText(GetPlayerDataPath(data.PlayerName)))
                    {
                        Sw.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
                    }
                    Debug.Log("[TERRA] " + data.PlayerName + " La database est sauvegardé !");
                }
                return data;
            }
            else
            {
                using (StreamReader Sr = new StreamReader(GetPlayerDataPath(player.username)))
                {
                    return JsonConvert.DeserializeObject<DataInfo>(Sr.ReadToEnd());
                }
            }
        }

        public void SaveData(DateTime date, string IP, string Admin, string player, string Password, string Reason, string deviceId = null)
        {
            EntityCollections.TryGetPlayerByNameOrID(player, out ShPlayer player1);
            DataInfo data = LoadData(player1);
            Ban ban = new Ban() { Admin = Admin, Date = date, Reason = Reason, deviceid = deviceId, ip = IP, password = Password, PlayerName = player };
            data.bans.Add(ban);
            using (StreamWriter Sw = new StreamWriter(GetPlayerDataPath(player)))
            {
                Sw.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            Debug.Log("[TERRA] " + $"La database est de {player} !");
        }
        //add ban in database
        public IEnumerator banning(ShPlayer player, ShPlayer target, int time, string reason)
        {
            yield return new WaitForSeconds(0.01f);

            var DeviceID = target.svPlayer.connectData.deviceID;
            var date = DateTime.Now.AddSeconds(time);
            var Ip = target.svPlayer.connection.IP;
            var admin = target.username;
            var name = player.username;
            var password = target.svPlayer.connectData.passwordHash.ToString();

            yield return new WaitForSeconds(0.01f);

            Ban ban = new Ban() { deviceid = DeviceID, ip = Ip, PlayerName = name, Admin = admin, Date = date, password = password, Reason = reason };
            Banned Bans = new Banned();
            SaveData(date, Ip, admin, name, password, reason, DeviceID);
            if (File.Exists(PatchBan))
            {
                using (StreamReader Sr = new StreamReader(PatchBan))
                {
                    Bans = JsonConvert.DeserializeObject<Banned>(Sr.ReadToEnd());
                }
                Bans.bans.Add(ban);
                using (StreamWriter Sw = File.CreateText(PatchBan))
                {
                    Sw.Write(JsonConvert.SerializeObject(Bans, Formatting.Indented));
                }
            }
            else
            {
                Bans.bans.Add(ban);
                using (StreamWriter Sw = File.CreateText(PatchBan))
                {
                    Sw.Write(JsonConvert.SerializeObject(Bans, Formatting.Indented));
                }
            }

            yield return new WaitForSeconds(0.1f);

            target.svPlayer.svManager.Disconnect(target.svPlayer.connection, DisconnectTypes.Banned);

            yield break;
        }

        public bool IsBanned(SvManager manager, ConnectData connectionData)
        {
            Banned Bans = new Banned();
            if (!File.Exists(PatchBan))
            {
                return true;
            }

            using (StreamReader Sr = new StreamReader(PatchBan))
            {
                Bans = JsonConvert.DeserializeObject<Banned>(Sr.ReadToEnd());
            }
            if (Bans.bans == null || Bans.bans.Count == 0)
            {
                return true;
            }
            else
            {
                foreach (Ban ban in Bans.bans)
                {
                    if (ban.ip == connectionData.connection.IP || ban.deviceid == connectionData.deviceID || ban.PlayerName == connectionData.username || ban.password == connectionData.passwordHash.ToString())
                    {
                        if (string.IsNullOrEmpty(connectionData.deviceID))
                        {
                            manager.RegisterFail(connectionData.connection, "comment ça tu modifie ton client ?");
                            Debug.Log("[Fedora] " + connectionData.username + " a essayé de se connecté avec un client modifié !");
                            return false;
                        }

                        if (ban.Date <= DateTime.Now)
                        {
                            Bans.bans.Remove(ban);
                            using (StreamWriter Sw = File.CreateText(PatchBan))
                            {
                                Sw.Write(JsonConvert.SerializeObject(Bans, Formatting.Indented));
                            }
                        }
                        else
                        {
                            manager.RegisterFail(connectionData.connection, "&4tu es banni du serveur jusqu'au : " + ban.Date);
                            Debug.Log("[Fedora] " + connectionData.username + " a essayé de se connecté alors qu'il est ban !");
                            manager.StartCoroutine(SlowAction(1f, "deco", connectionData));
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        public IEnumerator SlowAction(float time, string action, ConnectData data)
        {
            switch (action)
            {
                case "deco":
                    yield return new WaitForSeconds(time);
                    data.connection.Disconnect(data.connection.ID);
                    break;
            }
            yield break;
        }

        public int ComputeLevenshteinDistance(string source, string target)
        {
            if (source == target) return 0;
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            source = source.ToLower(CultureInfo.InvariantCulture);
            target = target.ToLower(CultureInfo.InvariantCulture);
            int[] v0 = new int[target.Length + 1];
            int[] v1 = new int[target.Length + 1];

            for (int i = 0; i < v0.Length; i++)
            {
                v0[i] = i;
            }

            for (int i = 0; i < source.Length; i++)
            {
                v1[0] = i + 1;
                for (int j = 0; j < target.Length; j++)
                {
                    var cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }
                for (int j = 0; j < v0.Length; j++)
                {
                    v0[j] = v1[j];
                }
            }

            return v1[target.Length];
        }

        public bool IsValideLicense()
        {
            if (LoadConfig().LicensePlugin == "Dpdqzzqld-5562315" && DateTime.Now != new DateTime(2022, 12, 11, 0, 0, 0)) return true;
            else if (LoadConfig().LicensePlugin == "Dpdqzzqld-5562" && DateTime.Now != new DateTime(2022, 15, 11, 0, 0, 0)) return true;
            else if (LoadConfig().LicensePlugin == "Dpdqzzqld-556425454" && DateTime.Now != new DateTime(2022, 11, 11, 0, 0, 0)) return true;
            else return false;
        }

        public double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) { return 0.0; }
            if ((source.Length == 0) || (target.Length == 0)) { return 0.0; }
            if (source == target) { return 1.0; }

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return 1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length));
        }
        public void StopVanish(ShPlayer player)
        {
            vanished.Remove(player);
            Vector3 position = player.GetPosition;
            int Place = player.GetPlace.GetIndex;
            player.svPlayer.Respawn();
            player.svPlayer.SvRestore(position, player.GetRotation, Place);
            player.svPlayer.SendGameMessage(LoadConfig().StopVanish);
        }

        public void StartVanish(ShPlayer player)
        {
            vanished.Add(player);
            player.svPlayer.Send(SvSendType.LocalOthers, PacketFlags.Reliable, ClPacket.DeactivateEntity, new object[] { player.ID });
            player.svPlayer.SendGameMessage(LoadConfig().StartVanish);
        }
    }
    public class ConfigInfo
    {
        public string LicensePlugin = "";
        public string WelcomeMessage = "&4Welcome By FedoraTeam";
        public bool Rain = true;
        public float SkyColorR = 0;
        public float SkyColorG = 0;
        public float SkyColorB = 0;
        public string StartVanish = "&2You are Invisible";
        public string StopVanish = "&4You are Visible";
        public int NickChangeCount = 1000;
        public string Back = "you've been return";
        public string SafezoneEnter = "&2[SAFEZONE] you're in SafeZone";
        public string SafezoneExit = "&4[SAFEZONE] you leaved the SafeZone";
        public string AlreadyAddTicket = "&4your previous ticket has been overwritten by the new";
        public string AddTicket = "&2Your ticket has been create";
        public bool CrimeOption = false;
        public bool LooseStuffOnDeath = false;
    }

    public class Warn
    {
        public string Admin { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
    }

    public class Ban
    {
        public string Admin { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; } = "no reason";
        public string PlayerName { get; set; }
        public string deviceid { get; set; }
        public string ip { get; set; }
        public string password { get; set; }
    }

    public class Fine
    {
        public string Author { get; set; }
        public string Count { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
    }

    public class Ticket
    {
        public string Author { get; set; }
        public string Count { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
    }

    public class Banned
    {
        public List<Ban> bans { get; set; } = new List<Ban>();
    }

    public class DataInfo
    {
        public string PlayerName { get; set; }
        public int Password { get; set; }
        public List<Ban> bans { get; set; } = new List<Ban>();
        public List<Ticket> tickets { get; set; } = new List<Ticket>();
        public List<Warn> warns { get; set; } = new List<Warn>();
        public List<Fine> fines { get; set; } = new List<Fine>();
    }
}
