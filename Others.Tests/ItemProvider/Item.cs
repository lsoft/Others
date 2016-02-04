namespace Others.Tests.ItemProvider
{
    internal class Item
    {
        public long Value
        {
            get;
            private set;
        }

        public Item(long value)
        {
            Value = value;
        }
    }
}