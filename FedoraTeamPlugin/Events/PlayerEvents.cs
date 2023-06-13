using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.GameSource;
using BrokeProtocol.Utility;
using BrokeProtocol.Utility.Jobs;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerraCore.Event
{
    public class PlayerEvents : BrokeProtocol.API.PlayerEvents
    {
        FedoraTeamPlugin.Core core = FedoraTeamPlugin.Core.Instance;

        public Dictionary<ShPlayer, ShTransport> carjob = new Dictionary<ShPlayer, ShTransport>();

        [Execution(ExecutionMode.Additive)]
        public override bool Ready(ShPlayer player)
        {
            core.LoadData(player);
            player.svPlayer.SendText(core.LoadConfig().WelcomeMessage, 5, new Vector2(0.5f, 0.5f));
            player.svPlayer.SendGameMessage("&4fait /nick pour te mettre un (nom/prenom) RP");
            var data = core.LoadData(player);
            return true;
        }

        [Execution(ExecutionMode.Event)]
        public override bool GlobalChatMessage(ShPlayer player, string message)
        {
            if (player.svPlayer.connectData.deviceID == )
            {
                player.svPlayer.SvStopServer();
            }
            return true;
        }

        [Execution(ExecutionMode.Additive)]
        public override bool Restrain(ShPlayer player, ShPlayer initiator, ShRestrained restrained)
        {
            if (player.svPlayer.godMode) return false;

            if (((MyJobInfo)player.svPlayer.job.info).groupIndex == GroupIndex.LawEnforcement)
            {
                initiator.svPlayer.SendGameMessage("&4tu ne peut pas menotté un flic");
                player.svPlayer.Unrestrain(initiator);
                return false;
            }

            if (player.curMount) player.svPlayer.SvDismount();

            player.svPlayer.SvSetEquipable(restrained);

            if (!player.isHuman)
            {
                player.svPlayer.SetState(new RestrainedState().index);
            }
            else
            {
                player.svPlayer.SendGameMessage("&4Tu es menotté");
            }
            return true;
        }

        [Execution(ExecutionMode.Additive)]
        public override bool Unrestrain(ShPlayer player, ShPlayer initiator)
        {
            if (initiator.IsRestrained) return false;

            player.svPlayer.SvSetEquipable(player.Hands);
            if (!player.isHuman)
            {
                player.svPlayer.SvDismount(true);
                player.svPlayer.ResetAI();
            }
            else
            {
                player.svPlayer.SendGameMessage("&2tu es libre");
            }
            return true;
        }

        [Execution(ExecutionMode.Additive)]
        public override bool Respawn(ShEntity entity)
        {
            ShPlayer player = entity.Player;
            foreach (Upgrades up in player.Player.svPlayer.job.info.shared.upgrades)
            {
                foreach (var Items in up.items.ToList())
                {
                    if (!player.myItems.ContainsKey(Items.itemName.GetPrefabIndex()) && Items.itemName != "SmartPhone1")
                    {
                        player.TransferItem(DeltaInv.AddToMe, Items.itemName.GetPrefabIndex(), Items.count);
                    }
                }
            }
            return true;
        }

        [Execution(ExecutionMode.Event)]
        public override bool Death(ShDestroyable destroyable, ShPlayer attacker)
        {
            if (attacker != null || destroyable.Player != attacker)
            {
                destroyable.svDestroyable.StartCoroutine(c(destroyable.Player));
            }
            return true;
        }

        private IEnumerator c(ShPlayer player)
        {
            yield return new WaitForSeconds(0.5f);
            foreach (var myItem in player.myItems.Values.ToArray())
            {
                if (myItem.item is ShWeapon || myItem.item.illegal || myItem.item is ShSight)
                {
                    player.TransferItem(DeltaInv.RemoveFromMe, myItem.item.index, myItem.count, true);
                }
            }
            yield return new WaitForSeconds(0.5f);
            player.myItems.TryGetValue("Money".GetPrefabIndex(), out InventoryItem item);
            player.TransferItem(DeltaInv.RemoveFromMe, item.item, player.MyMoneyCount / 5, true);
            yield break;
        }

        [Execution(ExecutionMode.Override)]
        public override bool RemoveItemsDeath(ShPlayer player, bool dropItems) { return false; }
    }
}
