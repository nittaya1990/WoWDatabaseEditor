using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.TrinityMySqlDatabase.Models;

namespace WDE.TrinityMySqlDatabase.Database;

public class TrinityWrathMySqlDatabaseProvider : BaseTrinityMySqlDatabaseProvider<TrinityWrathDatabase>
{
    public TrinityWrathMySqlDatabaseProvider(IWorldDatabaseSettingsProvider settings, IAuthDatabaseSettingsProvider authSettings, DatabaseLogger databaseLogger, ICurrentCoreVersion currentCoreVersion) : base(settings, authSettings, databaseLogger, currentCoreVersion)
    {
    }

    public override ICreatureTemplate? GetCreatureTemplate(uint entry)
    {
        using var model = Database();
        return model.CreatureTemplate.FirstOrDefault(ct => ct.Entry == entry);
    }

    public override IEnumerable<ICreatureTemplate> GetCreatureTemplates()
    {
        using var model = Database();
        return model.CreatureTemplate.OrderBy(t => t.Entry).ToList<ICreatureTemplate>();
    }
    
    public override async Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync()
    {
        await using var model = Database();
        return await model.CreatureTemplate.OrderBy(t => t.Entry).ToListAsync<ICreatureTemplate>();
    }

    public override async Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId)
    {
        await using var model = Database();
        return await model.GossipMenuOptions.Where(option => option.MenuId == menuId).ToListAsync<IGossipMenuOption>();
    }
    
    public override async Task<List<IBroadcastText>> GetBroadcastTextsAsync()
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts select t).ToListAsync<IBroadcastText>();
    }
    
    public override IBroadcastText? GetBroadcastTextByText(string text)
    {
        using var model = Database();
        return (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t).FirstOrDefault();
    }

    public override async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t).FirstOrDefaultAsync();
    }
    
    public override async Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id)
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts where t.Id == id select t).FirstOrDefaultAsync();
    }

    public override ICreature? GetCreatureByGuid(uint guid)
    {
        using var model = Database();
        return model.Creature.FirstOrDefault(c => c.Guid == guid);
    }

    public override IEnumerable<ICreature> GetCreaturesByEntry(uint entry)
    {
        using var model = Database();
        return model.Creature.Where(g => g.Entry == entry).ToList();
    }

    public override IEnumerable<ICreature> GetCreatures()
    {
        using var model = Database();
        return model.Creature.OrderBy(t => t.Entry).ToList<ICreature>();
    }

    public override async Task<List<ICreature>> GetCreaturesAsync()
    {
        await using var model = Database();
        return await model.Creature.OrderBy(t => t.Entry).ToListAsync<ICreature>();
    }

    public override async Task<IList<ITrinityString>> GetStringsAsync()
    {
        await using var model = Database();
        return await model.Strings.ToListAsync<ITrinityString>();
    }

    public override async Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync()
    {
        await using var model = Database();
        return await model.SpellDbc.ToListAsync<IDatabaseSpellDbc>();
    }
    
    protected override async Task SetCreatureTemplateAI(TrinityWrathDatabase model, uint entry, string ainame, string scriptname)
    {
        await model.CreatureTemplate.Where(p => p.Entry == entry)
            .Set(p => p.AIName, ainame)
            .Set(p => p.ScriptName, scriptname)
            .UpdateAsync();
    }

    protected override async Task<ICreature?> GetCreatureByGuid(TrinityWrathDatabase model, uint guid)
    {
        return await model.Creature.FirstOrDefaultAsync(e => e.Guid == guid);
    }
    
    public override async Task<List<IGameObject>> GetGameObjectsAsync()
    {
        await using var model = Database();
        return await model.GameObject.ToListAsync<IGameObject>();
    }

    public override IEnumerable<IGameObject> GetGameObjects()
    {
        using var model = Database();
        return model.GameObject.ToList<IGameObject>();
    }
    
    public override IGameObject? GetGameObjectByGuid(uint guid)
    {
        using var model = Database();
        return model.GameObject.FirstOrDefault(g => g.Guid == guid);
    }

    public override IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry)
    {
        using var model = Database();
        return model.GameObject.Where(g => g.Entry == entry).ToList();
    }
    
    protected override Task<IGameObject?> GetGameObjectByGuidAsync(TrinityWrathDatabase model, uint guid)
    {
        return model.GameObject.FirstOrDefaultAsync<IGameObject>(g => g.Guid == guid);
    }
    
    public override async Task<IList<ICreature>> GetCreaturesByMapAsync(uint map)
    {
        await using var model = Database();
        return await model.Creature.Where(c => c.Map == map).ToListAsync<ICreature>();
    }

    public override async Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map)
    {
        await using var model = Database();
        return await model.GameObject.Where(c => c.Map == map).ToListAsync<IGameObject>();
    }
}