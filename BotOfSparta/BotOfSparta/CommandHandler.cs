using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace BotOfSparta
{
    public class CommandHandling
    {
        #region Props

        public static CommandHandling Instance
        {
            get
            {
                if (_instance == null) _instance = new CommandHandling();
                return _instance;
            }
            private set { }
        }

        #endregion

        #region Fields

        private Dictionary<string, CommandHandler> HandlerList = new Dictionary<string, CommandHandler>();
        private CommandHandler UnknownCommand;
        private Bot ConnectedBot = null;

        private static CommandHandling _instance = null;

        #endregion

        #region Methods

        public void RegisterCommand(string command, CommandHandler handler)
        {
            HandlerList.Add(command, handler);
        }

        public void RegisterUnknownCommand(CommandHandler handler)
        {
            UnknownCommand = handler;
        }

        public bool AttemptCommandExecute(string command, User user, OnChatCommandReceivedArgs e)
        {
            CommandHandler handler = null;
            HandlerList.TryGetValue(command, out handler);

            if (handler != null)
                return handler.OnExecuteCommandRoot(user, e);
            else
                return UnknownCommand.OnExecuteCommandRoot(user, e);
        }

        public void SetBot(Bot bot)
        {
            ConnectedBot = bot;
        }

        public Bot GetBot()
        {
            return ConnectedBot;
        }

        #endregion
    }

    public class CommandHandler
    {
        #region Fields

        public uint NeededClearanceLevel = 0;

        #endregion

        #region Methods

        public bool OnExecuteCommandRoot(User user, OnChatCommandReceivedArgs e)
        {
            if (user.ClearanceLevel >= NeededClearanceLevel)
                return OnExecuteCommand(user, e);

            return false;
        }

        public virtual bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e) { return false; }

        #endregion
    }

    public class StatsCommandHandler : CommandHandler
    {
        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            if(e.Command.ArgumentsAsList.Count > 0)
            {
                FromSpecificUser(e.Command.ArgumentsAsList[0], user, e);
            }
            else
            {
                FromOwnUser(user, e);
            }

            return false;
        }

        private void FromOwnUser(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();

            uint goldBars = 0, goldCoins = 0, goldDimes = 0, playerTime = 0;

            ItemStack barStack = ItemStack.CreateStackWithItemID(1);
            ItemStack coinStack = ItemStack.CreateStackWithItemID(2);
            ItemStack dimeStack = ItemStack.CreateStackWithItemID(3);
            ItemStack timeStack = ItemStack.CreateStackWithItemID(4);

            ItemStack playerBarStack, playerCoinStack, playerDimeStack;
            if (DataBase.DB.InventoryLookup(user, barStack, out playerBarStack) > 0)
            {
                if (playerBarStack != null)
                    goldBars = playerBarStack.Amount;
            }
            if (DataBase.DB.InventoryLookup(user, coinStack, out playerCoinStack) > 0)
            {
                if(playerCoinStack != null)
                    goldCoins = playerCoinStack.Amount;
            }
            if (DataBase.DB.InventoryLookup(user, dimeStack, out playerDimeStack) > 0)
            {
                if (playerDimeStack != null)
                    goldDimes = playerDimeStack.Amount;
            }

            ItemStack playerTimeStack;
            if(DataBase.DB.InventoryLookup(user, timeStack, out playerTimeStack) > 0)
            {
                if (playerTimeStack != null)
                    playerTime = playerTimeStack.Amount;
            }

            uint hours = (uint)Math.Floor(playerTime / 60.0f);
            uint days = (uint)Math.Floor(hours / 24.0f);
            uint minutes = playerTime - hours * 60;
            hours = hours - days * 24;

            bot.Client.SendMessage(e.Command.ChatMessage.Channel, user.Username + @" > You have " + goldBars + " Gold Bars, " + goldCoins + " Gold Coins, " + goldDimes + " Gold Dimes. Watch Time: [ " + days + " days, " + hours + " hours, " + minutes + " mintes ]");
        }

        private void FromSpecificUser(string username, User otherUser, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();

            User user = DataBase.DB.UserLookupByName(username);

            if (!user.IsValid) return;

            uint goldBars = 0, goldCoins = 0, goldDimes = 0, playerTime = 0;

            ItemStack barStack = ItemStack.CreateStackWithItemID(1);
            ItemStack coinStack = ItemStack.CreateStackWithItemID(2);
            ItemStack dimeStack = ItemStack.CreateStackWithItemID(3);
            ItemStack timeStack = ItemStack.CreateStackWithItemID(4);

            ItemStack playerBarStack, playerCoinStack, playerDimeStack;
            if (DataBase.DB.InventoryLookup(user, barStack, out playerBarStack) > 0)
            {
                goldBars = playerBarStack.Amount;
            }
            if (DataBase.DB.InventoryLookup(user, coinStack, out playerCoinStack) > 0)
            {
                goldCoins = playerCoinStack.Amount;
            }
            if (DataBase.DB.InventoryLookup(user, dimeStack, out playerDimeStack) > 0)
            {
                goldDimes = playerDimeStack.Amount;
            }

            ItemStack playerTimeStack;
            if (DataBase.DB.InventoryLookup(user, timeStack, out playerTimeStack) > 0)
            {
                if (playerTimeStack != null)
                    playerTime = playerTimeStack.Amount;
            }

            uint hours = (uint)Math.Floor(playerTime / 60.0f);
            uint days = (uint)Math.Floor(hours / 24.0f);
            uint minutes = playerTime - hours * 60;
            hours = hours - days * 24;

            bot.Client.SendMessage(e.Command.ChatMessage.Channel, otherUser.Username + @" > " + user.Username + " has " + goldBars + " Gold Bars, " + goldCoins + " Gold Coins, " + goldDimes + " Gold Dimes. Watch Time: [ " + days + " days, " + hours + " hours, " + minutes + " mintes ]");
        }

        #endregion
    }

    public class GoOnlineHandler : CommandHandler
    {
        #region Initialization

        public GoOnlineHandler()
        {
            NeededClearanceLevel = 2;
        }

        #endregion

        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();
            if (!bot.Online)
            {
                bot.m_Treasures.ResetGoldTreasure();

                bot.GoOnline();
                bot.Client.SendMessage(e.Command.ChatMessage.Channel, "Bot is now awake and recording viewer stats!");
                return true;
            }
            return false;
        }

        #endregion
    }

    public class GoOfflineHandler : CommandHandler
    {
        #region Initialization

        public GoOfflineHandler()
        {
            NeededClearanceLevel = 2;
        }

        #endregion

        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();
            if (bot.Online)
            {
                bot.GoOffline();
                bot.Client.SendMessage(e.Command.ChatMessage.Channel, "Bot will now go to sleep! See you guys next time!");

                DataBase.DB.PullBackup();

                return true;
            }
            return false;
        }

        #endregion
    }

    public class SoundFXHandler : CommandHandler
    {
        #region Fields

        SoundFX m_Sounds = new SoundFX();

        #endregion

        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();
            if (bot.Online)
            {
                bool sub = e.Command.ChatMessage.IsSubscriber;
                var args = e.Command.ArgumentsAsList;
                if(args.Count <= 0) return false;
                var name = args[0];
                AudioRequestResult result = m_Sounds.CanPlay(name, user.ClearanceLevel >= 2, user.ClearanceLevel >= 2);
                bool canPlay = false;
                ItemStack costStack = null;
                if (result.Succes)
                {
                    if (user.ClearanceLevel >= 2 || sub)
                    {
                        canPlay = true;
                    }
                    else if (result.Cost.RequiredItemID == 0)
                    {
                        costStack = ItemStack.CreateStackWithItemID(result.Cost.CostItemID, result.Cost.CostAmount);
                        ItemStack playerStack;
                        uint invSlot = DataBase.DB.InventoryLookup(user, costStack, out playerStack);
                        if (invSlot > 0)
                        {
                            if (playerStack.Amount >= result.Cost.CostAmount)
                            {
                                canPlay = true;
                            }
                        }
                    }
                }

                if(canPlay)
                {
                    // Take item count from player
                    if (user.ClearanceLevel < 2 && !sub)
                        DataBase.DB.UnsafeTakeItem(user, costStack);
                    // Play audio and return message
                    AudioRequestResult playResult = m_Sounds.Play(name, user.ClearanceLevel >= 2, user.ClearanceLevel >= 2);
                    bot.Client.SendMessage(e.Command.ChatMessage.Channel, playResult.Message);
                }
            }
            return false;
        }

        #endregion
    }

    public class ConvertCommand : CommandHandler
    {
        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();

            if (e.Command.ArgumentsAsList.Count < 2) return false; // Too few arguments
            uint amountToConvert;
            string toConvert = e.Command.ArgumentsAsList[1];
            if (!uint.TryParse(e.Command.ArgumentsAsList[0], out amountToConvert)) return false; // Incorrect argument
            bool convertUp = (e.Command.ArgumentsAsList.Count >= 3);

            uint toConvertID = 0;

            switch(toConvert)
            {
                case "bars":
                    toConvertID = 1;
                    convertUp = false;
                    break;

                case "coins":
                    toConvertID = 2;
                    break;

                case "dimes":
                    toConvertID = 3;
                    convertUp = true;
                    break;
            }

            if (toConvertID <= 0) return false; // Incorrect argument

            ItemStack toFind = ItemStack.CreateStackWithItemID(toConvertID, amountToConvert);
            ItemStack playerStack;

            if (DataBase.DB.InventoryLookup(user, toFind, out playerStack) <= 0) return false; // User does not have this item

            if (playerStack.Amount < amountToConvert) return false; // Player does not have enough of the specified item

            if(!convertUp)
            {
                DataBase.DB.UnsafeTakeItem(user, toFind);
                uint convertToID = toConvertID + 1;

                ItemStack convertToStack = ItemStack.CreateStackWithItemID(convertToID, amountToConvert * 1000);

                DataBase.DB.GiveItem(user, convertToStack);

                bot.Client.SendMessage(e.Command.ChatMessage.Channel, "@" + user.Username + " > Converted " + amountToConvert + " " + toFind.BaseItem.ItemName + " to " + (convertToStack.Amount) + " " + convertToStack.BaseItem.ItemName + ".");
            }
            else
            {
                if (amountToConvert % 1000 != 0) return false; // Need to convert in pairs of 1000
                DataBase.DB.UnsafeTakeItem(user, toFind);
                uint convertToID = toConvertID - 1;

                ItemStack convertToStack = ItemStack.CreateStackWithItemID(convertToID, amountToConvert / 1000);

                DataBase.DB.GiveItem(user, convertToStack);

                bot.Client.SendMessage(e.Command.ChatMessage.Channel, "@" + user.Username + " > Converted " + amountToConvert + " " + toFind.BaseItem.ItemName + " to " + (convertToStack.Amount) + " " + convertToStack.BaseItem.ItemName + ".");
            }

            return false;
        }

        #endregion
    }

    public class UnknownCommandHandler : CommandHandler
    {
        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();

            var cmd = e.Command.CommandText;

            if(bot.m_Treasures.AttemptClaim(cmd))
            {
                var treasure = bot.m_Treasures.CurrentGoldTreasure;

                ItemStack barsReward = ItemStack.CreateStackWithItemID(1, treasure.Bars);
                ItemStack coinsReward = ItemStack.CreateStackWithItemID(2, treasure.Coins);
                ItemStack dimesReward = ItemStack.CreateStackWithItemID(3, treasure.Dimes);

                if (barsReward != null)
                    DataBase.DB.GiveItem(user, barsReward);

                if (coinsReward != null)
                    DataBase.DB.GiveItem(user, coinsReward);

                if (dimesReward != null)
                    DataBase.DB.GiveItem(user, dimesReward);

                bot.m_Treasures.ResetGoldTreasure();

                bot.Client.SendMessage(e.Command.ChatMessage.Channel, "@" + user.Username + " > Congratiulations! You were the first one to claim the treasure!");
            }

            return false;
        }

        #endregion
    }

    public class HelpCommandHandler : CommandHandler
    {
        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            var bot = CommandHandling.Instance.GetBot();
            if (e.Command.ArgumentsAsList.Count <= 0)
            {
                bot.Client.SendMessage(e.Command.ChatMessage.Channel, "Commands: !gold, !gold [user], !convert [amount] [dimes/coins/bars] !play [sfx]");
            }

            return false;
        }

        #endregion
    }

    public class SpawnItemCommandHandler : CommandHandler
    {
        #region Initialization

        public SpawnItemCommandHandler()
        {
            NeededClearanceLevel = 2;
        }

        #endregion

        #region Methods

        public override bool OnExecuteCommand(User user, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsList.Count < 3) return false;

            string userToGiveName = e.Command.ArgumentsAsList[0];
            uint amountToGive, itemID;
            if (!uint.TryParse(e.Command.ArgumentsAsList[1], out itemID)) return false;
            if (!uint.TryParse(e.Command.ArgumentsAsList[2], out amountToGive)) return false;

            ItemStack itemStack = ItemStack.CreateStackWithItemID(itemID, amountToGive);

            if (itemStack == null) return false;

            User userToGive = DataBase.DB.UserLookupByName(userToGiveName);

            if (!userToGive.IsValid) return false;

            DataBase.DB.GiveItem(userToGive, itemStack);

            return false;
        }

        #endregion
    }
}
