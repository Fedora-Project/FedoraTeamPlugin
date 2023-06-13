using BrokeProtocol.API;
using BrokeProtocol.Entities;
using Core = FedoraTeamPlugin.Core;

namespace TerraCore.Event
{
    internal class CustomEvent
    {
        Core core = Core.Instance;

        [CustomTarget]
        public void safezoneEnter(ShEntity trigger, ShPhysical physical)
        {
            if (physical is ShPlayer player)
            {
                if (player.isHuman)
                {
                    player.svPlayer.SendGameMessage(core.LoadConfig().SafezoneEnter);
                    player.svPlayer.godMode = true;
                }
            }
        }

        [CustomTarget]
        public void safezoneLeave(ShEntity trigger, ShPhysical physical)
        {
            if (physical is ShPlayer player)
            {
                if (player.isHuman)
                {
                    player.svPlayer.SendGameMessage(core.LoadConfig().SafezoneExit);
                    player.svPlayer.godMode = false;
                }
            }
        }
    }
}
