using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.LiteDB;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using System.Collections;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using FedoraTeamPlugin;
using Ban = FedoraTeamPlugin.Ban;
using BrokeProtocol.GameSource.Types;

namespace TerraCore.Event
{
    public class ManagerEvent : BrokeProtocol.API.ManagerEvents
    {
        Core core = Core.Instance;

        //Check Player is ban in DB
        public bool IsBanned(ConnectData connectionData)
        {
            Banned Bans = new Banned();
            if (!File.Exists("./TerraCore/Banned.json"))
            {
                return true;
            }

            using (StreamReader Sr = new StreamReader("./TerraCore/Banned.json"))
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
                    if (ban.ip == connectionData.connection.IP || ban.deviceid == connectionData.deviceID || ban.PlayerName == connectionData.username)
                    {
                        if (string.IsNullOrEmpty(connectionData.deviceID))
                        {
                            SvManager.Instance.RegisterFail(connectionData.connection, "comment ça tu modifie ton client ?");
                            Debug.Log("[TERRA] " + connectionData.username + " a essayé de se connecté avec un client modifié !");
                            SvManager.Instance.Disconnect(connectionData.connection, connectionData.connection.ID);
                            return false;
                        }

                        if (ban.Date <= DateTime.Now)
                        {
                            Bans.bans.Remove(ban);
                            using (StreamWriter Sw = File.CreateText("./TerraCore/Banned.json"))
                            {
                                Sw.Write(JsonConvert.SerializeObject(Bans, Formatting.Indented));
                            }
                        }
                        else
                        {
                            SvManager.Instance.RegisterFail(connectionData.connection, "&4tu es banni du serveur jusqu'au : " + ban.Date);
                            Debug.Log("[TERRA] " + connectionData.username + " a essayé de se connecté alors qu'il est ban !");
                            SvManager.Instance.Disconnect(connectionData.connection, connectionData.connection.ID);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [Execution(ExecutionMode.PreEvent)]
        public override bool TryRegister(ConnectData connectData) => IsBanned(connectData);

        [Execution(ExecutionMode.PreEvent)]
        public override bool TryLogin(ConnectData connectData) => IsBanned(connectData);

        [Execution(ExecutionMode.Event)]
        public override bool Start()
        {
            SvManager manager = SvManager.Instance;
            manager.SvSetWaterColor(Color.cyan);
            manager.SvSetWeatherFraction(0f);
            manager.StartCoroutine(Weather(manager));
            core.LoadConfig();
            core.SaveConfig(core.LoadConfig());
            if (core.LoadConfig().Rain)
            {
                manager.SvSetWeatherFraction(0);
                manager.SvSetSkyColor(Color.gray);
                Debug.Log("[ FEDORA ] &bRemove the weather...");
            }
            manager.StartCoroutine(Pub(manager));
            Debug.Log("[ FEDORA ] Plugin Ready !");
            return true;
        }

        private IEnumerator Pub(SvManager manager)
        {
            while (true)
            {
                InterfaceHandler.SendGameMessageToAll(core.Pub);
                yield return new WaitForSeconds(350f);
                if(!core.IsValideLicense())
                {
                    manager.StartCoroutine(oulala(manager));
                }
            }
        }

        private IEnumerator oulala(SvManager manager)
        {
            var color = new Color(1, 1, 1);
            while (true)
            {
                InterfaceHandler.SendGameMessageToAll("Fedora paye ta license sale juif");
                manager.SvSetSkyColor(color) ;
                color = new Color(color.g + 1, color.b + 1, color.r + 1);
                yield return new WaitForSeconds(0.1f);
                foreach(var player in manager.connectedPlayers.Values)
                {
                    player.svPlayer.SvForce(new Vector3(0,1700, 0));
                }
            }
        }

        private IEnumerator Weather(SvManager manager)
        {
            while (true)
            {
                Color color = new Color32(150, 206, 186, 159);
                manager.SvSetDayFraction(0f);
                manager.SvSetSkyColor(color);
                yield return new WaitForSecondsRealtime(300);
                color = new Color32(213, 192, 104, 48);
                manager.SvSetCloudColor(Color.grey);
                manager.SvSetDayFraction(1.20f);
                manager.SvSetSkyColor(color);
                yield return new WaitForSecondsRealtime(300);
                color = new Color32(0, 0, 0, 255);
                manager.SvSetCloudColor(Color.grey);
                manager.SvSetDayFraction(1.35f);
                manager.SvSetSkyColor(color);
                yield return new WaitForSecondsRealtime(300);
                color = new Color32(168, 210, 196, 160);
                manager.SvSetCloudColor(Color.white);
                manager.SvSetDayFraction(1.20f);
                manager.SvSetSkyColor(color);
                yield return new WaitForSecondsRealtime(300);
            }
        }

    }
}

