using System;
using System.Collections;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace BotOfSparta
{
    class TwitchInfo
    {
        #region Fields

        public string ClientID = "";
        public string ChannelName = "";
        public string BotUsername = "";
        public string BotToken = "";

        #endregion

        #region Initializtion

        public TwitchInfo() { }

        public static TwitchInfo CreateFromFile(string path)
        {
            TwitchInfo data;
            if (!JSONManager.Convert<TwitchInfo>(path, out data)) return null;
            return data;
        }

        #endregion
    }

    public class Bot
    {
        #region Fields


        private TwitchInfo _twitchInfo = TwitchInfo.CreateFromFile("Twitch.json");
        private ConnectionCredentials Credentials = null;
        public TwitchClient Client;
        private bool Running = true;
        public bool Online = false;
        private DateTime LastMinuteTime;

        public Treasures m_Treasures = new Treasures();

        private ArrayList CurrentViewers = new ArrayList();
        private ArrayList SeenUsers = new ArrayList();

        #endregion

        #region Connection

        public void Connect()
        {
            Credentials = new ConnectionCredentials(_twitchInfo.BotUsername, _twitchInfo.BotToken);

            Console.WriteLine("Connecting...");

            // Init
            Client = new TwitchClient();
            Client.Initialize(Credentials, _twitchInfo.ChannelName);

            // Message throttleing
            //Client.ChatThrottler = new TwitchLib.Client.Services.MessageThrottler(Client, 10, TimeSpan.FromSeconds(30));
            //Client.WhisperThrottler = new TwitchLib.Client.Services.MessageThrottler(Client, 10, TimeSpan.FromSeconds(30));

            // Events
            Client.OnJoinedChannel += onJoinedChannel;
            Client.OnMessageReceived += onMessageReceived;
            Client.OnWhisperReceived += onWhisperReceived;
            Client.OnNewSubscriber += onNewSubscriber;
            Client.OnReSubscriber += onReSubscriber;
            Client.OnConnected += Client_OnConnected;
            Client.OnUserLeft += onUserLeft;
            Client.OnChatCommandReceived += onChatCommand;

            // Connect
            Client.Connect();

            CommandHandling.Instance.SetBot(this);

            LastMinuteTime = DateTime.Now;
        }

        public bool Loop()
        {
            if (Online)
            {
                //CheckTreasure();

                DateTime now = DateTime.Now;
                TimeSpan difSpan = now.Subtract(LastMinuteTime);
                double diff = difSpan.TotalSeconds;

                if (60 <= diff)
                {
                    GiveToAll(2, 5);
                    GiveToAll(3, 50);
                    GiveToAll(4, 1);

                    LastMinuteTime = DateTime.Now;
                }
            }

            return Running;
        }

        #endregion

        #region Methods

        private void CheckTreasure()
        {
            if(m_Treasures.CheckTime(600))
            {
                m_Treasures.GenerateNewGoldTreasure();
                Client.SendMessage(_twitchInfo.ChannelName, "A treasure of " + m_Treasures.CurrentGoldTreasure.Bars + " Gold Bars, " + m_Treasures.CurrentGoldTreasure.Coins + " Gold Coins and " + m_Treasures.CurrentGoldTreasure.Dimes + " Gold Dimes has appeared! Type !" + m_Treasures.CurrentCommand + " to claim it!");
            }
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            Client.Disconnect();
        }

        public void GoOnline()
        {
            Online = true;
            CurrentViewers.Clear();
            SeenUsers.Clear();
        }

        public void GoOffline()
        {
            Online = false;
        }

        private void onUserLeft(object sender, OnUserLeftArgs e)
        {
            if (Online)
            {
                var name = e.Username;
                if (CurrentViewers.IndexOf(name) != -1)
                {
                    CurrentViewers.Remove(name);
                }
            }
        }

        private void onChatCommand(object sender, OnChatCommandReceivedArgs e)
        {
            AddViewer(e.Command.ChatMessage.Username, e.Command.ChatMessage.UserId);

            var twitchID = e.Command.ChatMessage.UserId;
            var userName = e.Command.ChatMessage.Username;
            var command = e.Command.CommandText;
            var user = DataBase.DB.UserLookup(userName, twitchID);

            CommandHandling.Instance.AttemptCommandExecute(command, user, e);
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void onJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Bot connected to channel!");
            Client.SendMessage(e.Channel, "The bot has arrived!");
        }

        private void onMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            //if (e.ChatMessage.Message.Contains("badword"))
            //Client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "Bad word! 30 minute timeout!");

            AddViewer(e.ChatMessage.Username, e.ChatMessage.UserId);
        }
        private void onWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            //if (e.WhisperMessage.Username == "my_friend")
                //Client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
        }
        private void onNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            uint goldBarsGift = 0;
            switch (e.Subscriber.SubscriptionPlan)
            {
                case SubscriptionPlan.Prime:
                    Client.SendMessage(e.Channel, $"{e.Subscriber.DisplayName} just subbed using Twitch Prime! Have a Gold bar as token of my gratitude! Thank you so much! <3");
                    goldBarsGift = 1;
                    break;

                case SubscriptionPlan.Tier1:
                    Client.SendMessage(e.Channel, $"{e.Subscriber.DisplayName} just subbed using a Tier1 subscription! Have a Gold bar as token of my gratitude! Thank you so much! <3 sparta8Kappa");
                    goldBarsGift = 1;
                    break;

                case SubscriptionPlan.Tier2:
                    Client.SendMessage(e.Channel, $"{e.Subscriber.DisplayName} just subbed using a Tier2 subscription! Have 2 Gold bars as token of my gratitude! Thank you so much!! <3 <3 sparta8Kappa");
                    goldBarsGift = 2;
                    break;

                case SubscriptionPlan.Tier3:
                    Client.SendMessage(e.Channel, $"{e.Subscriber.DisplayName} just subbed using a Tier3 subscription! Have 5 Gold bars as token of my gratitude! Thank you so much!!! <3 <3 <3 sparta8Kappa");
                    goldBarsGift = 5;
                    break;
            }

            var user = DataBase.DB.UserLookup(e.Subscriber.UserId);
            var stack = ItemStack.CreateStackWithItemID(1, goldBarsGift);
            DataBase.DB.GiveItem(user, stack);
        }

        private void onReSubscriber(object sender, OnReSubscriberArgs e)
        {
            uint goldBarsGift = 0;
            switch (e.ReSubscriber.SubscriptionPlan)
            {
                case SubscriptionPlan.Prime:
                    Client.SendMessage(e.Channel, $"{e.ReSubscriber.DisplayName} just re-subbed using Twitch Prime for {e.ReSubscriber.Months} months in a row! Have a Gold bar as token of my gratitude! Thank you so much! <3 sparta8Kappa");
                    goldBarsGift = 1;
                    break;

                case SubscriptionPlan.Tier1:
                    Client.SendMessage(e.Channel, $"{e.ReSubscriber.DisplayName} just re-subbed using a Tier1 subscription for {e.ReSubscriber.Months} months in a row! Have a Gold bar as token of my gratitude! Thank you so much! <3 sparta8Kappa");
                    goldBarsGift = 1;
                    break;

                case SubscriptionPlan.Tier2:
                    Client.SendMessage(e.Channel, $"{e.ReSubscriber.DisplayName} just re-subbed using a Tier2 subscription for {e.ReSubscriber.Months} months in a row! Have 2 Gold bars as token of my gratitude! Thank you so much!! <3 <3 sparta8Kappa");
                    goldBarsGift = 2;
                    break;

                case SubscriptionPlan.Tier3:
                    Client.SendMessage(e.Channel, $"{e.ReSubscriber.DisplayName} just re-subbed using a Tier3 subscription for {e.ReSubscriber.Months} months in a row! Have 5 Gold bars as token of my gratitude! Thank you so much!!! <3 <3 <3 sparta8Kappa");
                    goldBarsGift = 5;
                    break;
            }

            var user = DataBase.DB.UserLookup(e.ReSubscriber.UserId);
            var stack = ItemStack.CreateStackWithItemID(1, goldBarsGift);
            DataBase.DB.GiveItem(user, stack);
        }

        public void GiveToAll(uint itemID, uint amount)
        {
            foreach(string viewer in CurrentViewers)
            {
                var user = DataBase.DB.UserLookupByName(viewer);
                var stack = ItemStack.CreateStackWithItemID(itemID, amount);
                DataBase.DB.GiveItem(user, stack);
            }
        }

        private void AddViewer(string name, string twitchID)
        {
            if (Online)
            {
                if (CurrentViewers.IndexOf(name) == -1)
                {
                    CurrentViewers.Add(name);
                    if (SeenUsers.IndexOf(name) == -1)
                    {
                        SeenUsers.Add(name);
                        var user = DataBase.DB.UserLookup(name, twitchID);
                        if (user.IsValid)
                        {
                            var stack = ItemStack.CreateStackWithItemID(2, 50); // 50 Free coins per stream
                            var stack2 = ItemStack.CreateStackWithItemID(3, 500); // 500 Free dimes per stream
                            DataBase.DB.GiveItem(user, stack);
                            DataBase.DB.GiveItem(user, stack2);

                            Console.WriteLine(name + " joined the stream for the first time today and thus claimed his daily reward!");
                        }
                    }
                    else
                    {
                        Console.WriteLine(name + " joined the stream");
                    }
                }
            }
        }

        #endregion
    }
}
