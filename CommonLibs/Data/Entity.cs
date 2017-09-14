

namespace CommonLibs
{
    public abstract class Entity
    {
        public abstract int GetId();
        public abstract string GetKey();
        public abstract System.DateTime TryUpdateTime();
    }
}
