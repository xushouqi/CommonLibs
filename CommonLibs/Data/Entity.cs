

namespace CommonLibs
{
    public enum EntityActionEnum
    {
        None,
        Add,
        Update,
        Remove,
    }

    public abstract class Entity<TKey>
    {
        [DataView(Key = true)]
        public TKey Key { get; set; }

        public System.DateTime UpdateTime { get; set; }

        public EntityActionEnum Action = EntityActionEnum.None;
    }
}
