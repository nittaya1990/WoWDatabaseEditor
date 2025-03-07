using System;
using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class CombatCreatureSpellStatsProvider : IPacketDumperProvider
    {
        private readonly Func<SpellStatsDumper> dumper;
        public string Name => "Creature combat spell stats";
        public string Description => "Prints average combat spell timings";
        public ImageUri? Image { get; } = new ImageUri("Icons/creature_spell.png");
        public string Extension => "txt";

        public CombatCreatureSpellStatsProvider(Func<SpellStatsDumper> dumper)
        {
            this.dumper = dumper;
        }
        
        public Task<IPacketTextDumper> CreateDumper()
        {
            return Task.FromResult<IPacketTextDumper>(dumper());
        }
    }
}