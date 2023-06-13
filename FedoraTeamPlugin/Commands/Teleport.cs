using BrokeProtocol.API;
using BrokeProtocol.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedoraTeamPlugin.Commands
{
    internal class Teleport : IScript
    {
        Core core = Core.Instance;
        Dictionary<ShPlayer, Transform> Ppos = new Dictionary<ShPlayer, Transform>();
        private Teleport()
        {
            CommandHandler.RegisterCommand(new List<string> { "tv", "teleport", "tpv" }, new Action<ShPlayer, ShPlayer>(TeleportHandle));
            CommandHandler.RegisterCommand(new List<string> { "back", "b", "retour" }, new Action<ShPlayer>(Back));
            CommandHandler.RegisterCommand(new List<string> { "tv", "teleport", "tpv" }, new Action<ShPlayer, ShPlayer>(SummonHandle));
        }

        private void TeleportHandle(ShPlayer player, ShPlayer target)
        {
            if (Ppos.ContainsKey(player))
            {
                Ppos.Remove(player);
            }
            Ppos.Add(player, player.transform);
            player.svPlayer.SvTeleport(target);
            core.StopVanish(player);
            core.StartVanish(player);
        }

        private void SummonHandle(ShPlayer player, ShPlayer target)
        {
            if (Ppos.ContainsKey(target))
            {
                Ppos.Remove(target);
            }
            Ppos.Add(target, target.transform);
            target.svPlayer.SvTeleport(player);
            core.StopVanish(target);
        }

        private void Back(ShPlayer player)
        {
            if (Ppos.ContainsKey(player))
            {
                Ppos.TryGetValue(player, out Transform p);
                player.svPlayer.SvRestore(p.position, p.rotation, p.parent.GetInstanceID());
            }
            else
            {
                player.svPlayer.SendGameMessage(core.LoadConfig().Back);
            }
        }
    }
}
