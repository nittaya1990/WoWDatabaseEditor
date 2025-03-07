namespace TheEngine.ECS
{
    public interface IChunkDataIterator
    {
        int Length { get; }
        Entity this[int index] { get; }
        ComponentDataAccess<T> DataAccess<T>() where T : unmanaged, IComponentData;
    }
}