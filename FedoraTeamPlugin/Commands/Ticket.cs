using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.Required;
using BrokeProtocol.Utility;
using JetBrains.Annotations;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FedoraTeamPlugin.Commands
{
    public class ticket
    {
        public ShPlayer Owner { get; set; }
        public Dictionary<ShPlayer, Vector3> Players = new Dictionary<ShPlayer, Vector3>();
        public DateTime date { get; set; }
        public string reason { get; set; }
        public ShPlayer staff { get; set; }
    }
    internal class Ticket : IScript
    {
        Core core = Core.Instance;
        Dictionary<ShPlayer,ticket> Tickets = new Dictionary<ShPlayer,ticket>();
        LabelID[] actions = new LabelID[] { new LabelID("click", "click") };
        public Ticket()
        {
            CommandHandler.RegisterCommand(new List<string> { "tck", "reports", "tickets" }, new Action<ShPlayer>(ListTickets));
            CommandHandler.RegisterCommand(new List<string> { "ticket", "//", "report" }, new Action<ShPlayer, string>(AddTicket));
        }
        
        public void AddTicket(ShPlayer player, string reason)
        {
            Dictionary<ShPlayer, Vector3> players = new Dictionary<ShPlayer, Vector3>();
            players.Add(player, player.GetPosition);
            ticket ticket = new ticket()
            {
                date = DateTime.Now,
                reason = reason,
                Players = players
            };
            if (Tickets.ContainsKey(player))
            {
                Tickets.Remove(player);
                Tickets.Add(player, ticket);
                player.svPlayer.SendGameMessage(core.LoadConfig().AlreadyAddTicket);
            }
            else
            {
                Tickets.Add(player, ticket );
                player.svPlayer.SendGameMessage(core.LoadConfig().AddTicket);
            }
        }
        public void ListTickets(ShPlayer player)
        {
            List<LabelID> options = new List<LabelID>();
            foreach (var ticket in Tickets)
            {
                options.Add(new LabelID($"&1{ticket.Key.username} : &7{ ticket.Value.reason }", ticket.Key.username));
            }
            player.svPlayer.SendOptionMenu("&4Tickets List", player.ID, "ticketlist", options.ToArray(), actions);
        }

        [Target(GameSourceEvent.PlayerOptionAction, ExecutionMode.Event)]
        public void OptionAction(ShPlayer player, int targetID, string menuID, string optionID, string actionID)
        {
            if (menuID == "ticketlist")
            {
                List<LabelID> options;
                if (EntityCollections.TryGetPlayerByNameOrID(optionID, out ShPlayer p))
                {
                    Tickets.TryGetValue(p, out ticket ticket);
                    ticket.staff = player;
                    ticket.Players.TryGetValue(p, out Vector3 vector);
                    ticket.Players.Add(p, vector);
                    ticket.Players.Add(player, player.GetPosition);
                    Tickets.Remove(p);
                    Tickets.Add(player, ticket);
                    options = TicketOptions(ticket.reason, ticket.date);
                }
                else
                {
                    Tickets.TryGetValue(player, out ticket ticket);
                    options = TicketOptions(ticket.reason, ticket.date);
                }
                player.svPlayer.SendOptionMenu($"Ticket de : {p.username}", player.ID, "ticketplayer", options.ToArray(), actions);
            }
            else if (menuID == "ticketplayer")
            {
                Tickets.TryGetValue(player, out ticket ticket);
                switch (optionID)
                {
                    case "Add":
                        player.svPlayer.SendInputMenu("&2search player", player.ID, "sendplayerinput", UnityEngine.UI.InputField.ContentType.Name);
                        break;
                    case "Members":
                        List<LabelID> options = new List<LabelID>();

                        Tickets.TryGetValue(player, out ticket t);
                        foreach (ShPlayer p in t.Players.Keys)
                        {
                            options.Add(new LabelID(p.username, p.username));
                        }
                        player.svPlayer.SendOptionMenu("&2Member in ticket", player.ID, "playersList", options.ToArray(), actions);
                        break;
                    case "Return":
                        break;
                }
            }
            else if (menuID == "Members")
            {
                var players = EntityCollections.Accounts.Values;
                ShPlayer p = players.ToList().Find(x => x.username == optionID);
                Tickets.TryGetValue(player, out ticket ticket);
                ticket.Players.Remove(p);
            }
        }

        private List<LabelID> TicketOptions(string reason, DateTime date)
        {
            List<LabelID> options = new List<LabelID>();
            options.Add(new LabelID(reason, reason));
            options.Add(new LabelID($"&7{date.ToShortDateString()}", date.ToString()));
            options.Add(new LabelID("&2Add", "Add"));
            options.Add(new LabelID("&1Members", "Members"));
            options.Add(new LabelID("&9Return", "Return"));
            options.Add(new LabelID("&3Close", "Close"));
            return options;
        }

        [Target(GameSourceEvent.PlayerSubmitInput, ExecutionMode.Event)]
        public void InputAction(ShPlayer player, int targetID, string menuID, string input)
        {
            if(menuID == "sendplayerinput")
            {
                var players = EntityCollections.Players;
                ShPlayer target = players.OrderByDescending(x => core.CalculateSimilarity(x.username, input)).FirstOrDefault();
                if (target is null)
                {
                    player.svPlayer.SendGameMessage("&4joueur non trouvé !");
                }
                else
                {
                    player.svPlayer.SendGameMessage("&4joueur ajouté au ticket !");
                    Tickets.TryGetValue(player, out ticket t);
                    t.Players.Add(target, target.GetPosition);
                    target.svPlayer.SvRestore(player.GetPosition, player.GetRotation, player.GetPlace.GetIndex);
                    player.svPlayer.SendOptionMenu($"Ticket de : {t.Owner.username}", player.ID, "ticketplayer", TicketOptions(t.reason, t.date).ToArray(), actions);
                }
            }
        }
    }
}
