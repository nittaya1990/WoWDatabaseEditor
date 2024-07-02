using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.MapSpawnsEditor.Models;

[UniqueProvider]
public interface ISpawnSelectionService
{
    ReactiveProperty<SpawnInstance?> SelectedSpawn { get; }
    bool IsSelected(SpawnInstance spawnInstance);
}

[SingleInstance]
[AutoRegister]
public class SpawnSelectionService : ISpawnSelectionService
{
    public ReactiveProperty<SpawnInstance?> SelectedSpawn { get; } = new(null);

    public bool IsSelected(SpawnInstance spawnInstance) => SelectedSpawn.Value == spawnInstance;
}