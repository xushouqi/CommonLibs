

namespace CommonLibs
{
    public enum EntityActionEnum
    {
        None,
        Add,
        Update,
        Remove,
    }

    public abstract class Entity
    {
        public abstract int GetId();
        public abstract void SetId(int id);
        //public abstract string GetKey();
        public abstract System.DateTime TryUpdateTime();

        public EntityActionEnum Action = EntityActionEnum.None;
    }
}
