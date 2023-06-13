using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.Utility.Networking;
using ENet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedoraTeamPlugin.Commands
{
    public class Vanish : IScript
    {
        Core core = Core.Instance;

        public Vanish()
        {
            CommandHandler.RegisterCommand(new List<string> { "v", "vanish", "invisible" }, new Action<ShPlayer>(HandlerVanish));
        }

        private void HandlerVanish(ShPlayer player)
        {
            if (core.vanished.Contains(player))
            {
                core.StopVanish(player);
            }
            else
            {
                core.StartVanish(player);
            }
        }
    }
}
