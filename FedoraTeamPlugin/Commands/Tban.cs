using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.Utility.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FedoraTeamPlugin.Commands
{
    internal class Tban : IScript
    {
        Core core = Core.Instance;
        public Tban()
        {
            CommandHandler.RegisterCommand(new List<string> { "tban", "tempban", "ban" }, new Action<ShPlayer, string, int, string>(AddBan));
        }

        //search player to ban
        public void AddBan(ShPlayer player, string targetname, int time, string reason)
        {
            var players = EntityCollections.Accounts.Values;
            // search player in account database
            ShPlayer target = players.OrderByDescending(x => core.CalculateSimilarity(x.username, targetname)).FirstOrDefault();
            if (target is null)
            {
                player.svPlayer.SendGameMessage("&4joueur non trouvé !");
            }
            else
            {
                player.svPlayer.StartCoroutine(core.banning(player, target, time, reason));
            }
        }
    }
}