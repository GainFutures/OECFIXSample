namespace OEC.FIX.Sample.FoxScript.AllocationBlocks
{
    internal class AllocationBlockItem
    {
        public double Weight;
    }

    internal class PreAllocationBlockItem : AllocationBlockItem
    {
        public string Account;
    }

    internal class PostAllocationBlockItem : AllocationBlockItem
    {
        public ExtendedAccount Account = new ExtendedAccount();
        public double Price;
    }
}