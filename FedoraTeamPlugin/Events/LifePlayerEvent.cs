using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.GameSource;
using BrokeProtocol.GameSource.Types;

namespace TerraCore.Event
{
    public class LifePlayerEvent : LifeSourcePlayer
    {
        FedoraTeamPlugin.Core core = FedoraTeamPlugin.Core.Instance;
        public LifePlayerEvent(ShPlayer player) : base(player)
        {
        }

        [Execution(ExecutionMode.Override)]
        public override void AddCrime(CrimeIndex crimeIndex, ShPlayer victim)
        {
            if (!core.LoadConfig().CrimeOption)
            {
                return;
            }
        }
    }
}