using System;
using System.Collections.Generic;

namespace OEC.FIX.Sample.FoxScript.AllocationBlocks
{
    internal class AllocationBlock<TItem>
    {
        private const int MaxNameLength = 256;
        private readonly List<TItem> _items = new List<TItem>();
        public AllocationRule Rule;
        private string _name;

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    return string.Empty;
                return _name.Substring(0, Math.Min(_name.Length, MaxNameLength));
            }
            set { _name = value; }
        }

        public IEnumerable<TItem> Items => _items;

        public void Add(TItem item)
        {
            _items.Add(item);
        }
    }
}