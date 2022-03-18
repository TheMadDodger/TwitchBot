using System;

namespace BotOfSparta
{
    public class Treasures
    {
        #region Fields

        public GoldTreasure CurrentGoldTreasure = new GoldTreasure();
        public string CurrentCommand;
        private bool GoldTreasure = false;
        private DateTime TimeSinceLastGoldTreasure = DateTime.Now;

        #endregion

        #region Methods

        public void ResetGoldTreasure()
        {
            GoldTreasure = false;
            TimeSinceLastGoldTreasure = DateTime.Now;
            CurrentCommand = "";
        }

        public bool CheckTime(double neededDifference)
        {
            DateTime now = DateTime.Now;
            TimeSpan difSpan = now.Subtract(TimeSinceLastGoldTreasure);
            double diff = difSpan.TotalSeconds;

            return (neededDifference <= diff);
        }

        public void GenerateNewGoldTreasure()
        {
            CurrentGoldTreasure.Generate();

            var r = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds);
            int cmdID = r.Next(0, 4);

            switch(cmdID)
            {
                case 0:
                    CurrentCommand = "300";
                    break;

                case 1:
                    CurrentCommand = "forsparta";
                    break;

                case 2:
                    CurrentCommand = "forfreedom";
                    break;

                case 3:
                    CurrentCommand = "noretreatnosurrender";
                    break;

                case 4:
                    CurrentCommand = "thisissparta";
                    break;
            }

            GoldTreasure = true;
            TimeSinceLastGoldTreasure = DateTime.Now;
        }

        public bool AttemptClaim(string command)
        {
            if (!GoldTreasure) return false;
            return (CurrentCommand == command);
        }

        #endregion
    }

    #region Helper Structs

    public struct GoldTreasure
    {
        public uint Dimes;
        public uint Coins;
        public uint Bars;

        public void Generate()
        {
            var r = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds);
            Dimes = (uint)r.Next(100, 10000);
            Coins = (uint)r.Next(10, 1000);
            Bars = (uint)r.Next(0, 1);
        }
    }

    public struct RandomItemChest
    {
        public ItemStack Open()
        {
            return null;
        }
    }

    #endregion
}
