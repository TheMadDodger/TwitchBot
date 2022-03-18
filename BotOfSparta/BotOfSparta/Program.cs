using System;

namespace BotOfSparta
{
    class Program
    {
        #region Methods

        static void Main(string[] args)
        {
            if (DataBase.DB.Connect())
            {
                Bot bot = new Bot();
                bot.Connect();
                
                RegisterCommands();
                
                while (bot.Loop()) { }
                
                bot.Disconnect();
            }
            
            DataBase.DB.Disconnect();
            
            Console.ReadLine();
        }

        static private void RegisterCommands()
        {
            CommandHandling.Instance.RegisterCommand("gold", new StatsCommandHandler());
            CommandHandling.Instance.RegisterCommand("golive", new GoOnlineHandler());
            CommandHandling.Instance.RegisterCommand("offline", new GoOfflineHandler());
            CommandHandling.Instance.RegisterCommand("play", new SoundFXHandler());
            CommandHandling.Instance.RegisterCommand("convert", new ConvertCommand());
            CommandHandling.Instance.RegisterCommand("commands", new HelpCommandHandler());
            CommandHandling.Instance.RegisterCommand("spawn", new SpawnItemCommandHandler());
            CommandHandling.Instance.RegisterUnknownCommand(new UnknownCommandHandler());
        }

        #endregion
    }
}
