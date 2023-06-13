using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using System;

namespace FedoraTeamPlugin.Commands
{
    internal class Nick : IScript
    {
        Core core = Core.Instance;
        public Nick()
        {
            CommandHandler.RegisterCommand("nick", new Action<ShPlayer, string, string>(SetNick));
            CommandHandler.RegisterCommand("fnick", new Action<ShPlayer, string, string, ShPlayer>(fSetNick));
            CommandHandler.RegisterCommand("feedall", new Action<ShPlayer>(feedall));
            CommandHandler.RegisterCommand("healall", new Action<ShPlayer>(healall));
            CommandHandler.RegisterCommand("killall", new Action<ShPlayer>(killall));
        }

        private void SetNick(ShPlayer player, string name, string name2)
        {
            if (player.MyMoneyCount >= 1000)
            {
                player.svPlayer.SvUpdateDisplayName(name + " " + name2);
                player.TransferMoney(DeltaInv.RemoveFromMe, core.LoadConfig().NickChangeCount);
            }
            else
            {
                player.svPlayer.SendGameMessage($"vous n'avez pas {core.LoadConfig().NickChangeCount}$");
            }
        }

        private void fSetNick(ShPlayer player, string name, string name2, ShPlayer other)
        {
            other.svPlayer.SvUpdateDisplayName(name + " " + name2);
        }

        public void killall(ShPlayer player)
        {
            foreach (ShPlayer p in player.svPlayer.svManager.connectedPlayers.Values)
            {
                p.svPlayer.Damage(BrokeProtocol.Required.DamageIndex.Collision, p.health * 20);
            }
        }
        public void healall(ShPlayer player)
        {
            foreach (ShPlayer p in player.svPlayer.svManager.connectedPlayers.Values)
            {
                p.svPlayer.SvHeal(p);
            }
        }
        public void feedall(ShPlayer player)
        {
            foreach (ShPlayer p in player.svPlayer.svManager.connectedPlayers.Values)
            {
                for (int i = 1; i < p.maxStat; i++)
                {
                    p.stats.SetValue(p.maxStat, i);
                }
            }
        }
    }
}
