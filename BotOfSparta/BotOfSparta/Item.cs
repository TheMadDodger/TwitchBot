using System.Collections;

namespace BotOfSparta
{
    public class Item
    {
        public uint ItemID;
        public string ItemName;
        public string ItemDesc;
        public bool Stackable;
        public string DefaultMetaData;
    }

    public class ItemStack
    {
        #region Fields

        public Item BaseItem;
        public uint Amount;
        public string MetaData;

        #endregion

        #region Methods

        static public ItemStack CreateStackWithItemID(uint itemID, uint amount = 1)
        {
            if (amount == 0) return null;

            var item = DataBase.DB.UnsafeItemLookup(itemID);

            if (item == null) return null;

            var itemStack = new ItemStack();
            itemStack.BaseItem = item;
            itemStack.MetaData = item.DefaultMetaData;
            itemStack.Amount = amount;

            return itemStack;
        }

        #endregion
    }
}