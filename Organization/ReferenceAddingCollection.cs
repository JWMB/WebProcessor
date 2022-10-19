using Organization.Entities;
using System.Collections;

namespace Organization
{
    public class ReferenceAddingCollection<TParent, TChild> : ICollection<TChild>
    {
        private readonly List<TChild> children = new List<TChild>();
        protected readonly TParent parent;
        private readonly Action<TChild, TParent> setParent;

        public ReferenceAddingCollection(TParent parent, Action<TChild, TParent> setParent)
        {
            this.parent = parent;
            this.setParent = setParent;
        }

        #region defaults
        public void Clear() => children.Clear();
        public bool Contains(TChild item) => children.Contains(item);
        public virtual bool Remove(TChild item) => children.Remove(item);
        public void CopyTo(TChild[] array, int arrayIndex)
        {
            for (int i = 0; i < children.Count; i++)
                array[arrayIndex++] = (TChild)children[i];
        }
        public IEnumerator<TChild> GetEnumerator() => children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();
        public int Count => children.Count;
        public bool IsReadOnly => false;
        #endregion

        public void AddRange(IEnumerable<TChild> items) => items.ToList().ForEach(Add);

        public void Add(TChild item)
        {
            setParent(item, parent);
            children.Add(item);
        }

        public T AddReturn<T>(T item) where T : TChild
        {
            Add(item);
            return item!;
        }
    }

    public class ReferenceAddingCollectionFromDefinition<TParent, TChild, TChildDef> : IEnumerable<TChild>
    {
        private readonly List<TChild> children = new List<TChild>();
        protected readonly TParent parent;
        private readonly Func<TChildDef, TParent, TChild> createChildSetParent;

        public ReferenceAddingCollectionFromDefinition(TParent parent, Func<TChildDef, TParent, TChild> createChildSetParent, IEnumerable<TChildDef>? children = null)
        {
            this.parent = parent;
            this.createChildSetParent = createChildSetParent;
            children?.ToList().ForEach(o => Add(o));
        }

        #region defaults
        public void Clear() => children.Clear();
        public bool Contains(TChild item) => children.Contains(item);
        public virtual bool Remove(TChild item) => children.Remove(item);
        public void CopyTo(TChild[] array, int arrayIndex)
        {
            for (int i = 0; i < children.Count; i++)
                array[arrayIndex++] = (TChild)children[i];
        }
        public IEnumerator<TChild> GetEnumerator() => children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();
        public int Count => children.Count;
        public bool IsReadOnly => false;
        #endregion

        //public void AddRange(IEnumerable<TChild> items) => items.ToList().ForEach(Add);

        public TChild Add(TChildDef item)
        {
            var child = createChildSetParent(item, parent);
            children.Add(child);
            return child;
        }
    }
}
