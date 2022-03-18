using System;
using System.Diagnostics;
using System.Text;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace BotOfSparta
{
    public class DatabaseInfo
    {
        #region Fields

        public string User = "";
        public string ServerIP = "";
        public int Port = 0;
        public string Password = "";
        public string DatabaseName = "";
        public string BackupLocation = "";

        #endregion

        #region Initializtion

        public DatabaseInfo() { }

        public static DatabaseInfo CreateFromFile(string path)
        {
            DatabaseInfo data;
            if (!JSONManager.Convert<DatabaseInfo>(path, out data)) return null;
            return data;
        }

        #endregion
    }

    class DataBase
    {
        #region Fields

        MySqlConnection DBConnection;
        MySqlCommand DBCommand = new MySqlCommand();
        MySqlCommand DBPreparedCommand = new MySqlCommand();

        static private DataBase Instance = null;

        private DatabaseInfo _databaseInfo = DatabaseInfo.CreateFromFile("Database.json");

        #endregion

        #region Props

        static public DataBase DB
        {
            get
            {
                if(Instance == null)
                    Instance = new DataBase();

                return Instance;
            }
        }

        #endregion

        #region Connection

        public bool Connect()
        {
            if (DBConnection == null)
            {
                string connectionString = string.Format("Server={0}; database=STREAM; UID={1}; password={2}; SslMode=none", _databaseInfo.ServerIP, _databaseInfo.User, _databaseInfo.Password);
                DBConnection = new MySqlConnection(connectionString);
                try
                {
                    DBConnection.Open();
                }
                catch (MySqlException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            return true;
        }

        public void Disconnect()
        {
            if (DBConnection != null)
            {
                DBConnection.Close();
            }
        }

        #endregion

        #region Methods

        public bool GiveItem(User user, ItemStack itemStack)
        {
            if (!user.IsValid) return false;
            var invSlot = InventoryLookup(user, itemStack);
            if(invSlot == 0)
            {
                // Create new inventory record
                return CreateInventoryRecord(user, itemStack);
            }
            else
            {
                // Add to existing
                try
                {
                    DBCommand.CommandText = "update inventory set amount=amount+" + itemStack.Amount + " where rwID=" + invSlot;
                    DBCommand.Connection = DBConnection;
                    DBCommand.ExecuteNonQuery();
                    return true;
                }
                catch(MySqlException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
        }

        public bool UnsafeTakeItem(User user, ItemStack itemStack)
        {
            if (!user.IsValid) return false;
            var invSlot = InventoryLookup(user, itemStack);
            if (invSlot == 0)
            {
                // Cant take what the player doesnt have!
                return false;
            }
            else
            {
                // Remove from existing
                try
                {
                    DBCommand.CommandText = "update inventory set amount=amount-" + itemStack.Amount + " where rwID=" + invSlot;
                    DBCommand.Connection = DBConnection;
                    DBCommand.ExecuteNonQuery();
                    return true;
                }
                catch (MySqlException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
        }

        public User UserLookup(string twitchID)
        {
            var data = UnsafeSelect("select * from users where twitchID=\"" + twitchID + "\";");
            var userData = new User();
            userData.IsValid = false;
            if (data != null)
            {
                while (data.Read())
                {
                    userData.UserID = data.GetUInt32("userID");
                    userData.Username = data.GetString("username");
                    userData.TwitchID = data.GetString("twitchID");
                    userData.CurrentTitle = data.GetInt32("currentTitle");
                    userData.ClearanceLevel = data.GetUInt32("clearanceLevel");
                    userData.IsValid = true;
                    break;
                }

                data.Close();
            }

            return userData;
        }

        public User UserLookupByName(string userName)
        {
            var data = UnsafeSelect("select * from users where username=\"" + userName + "\";");
            var userData = new User();
            userData.IsValid = false;
            if (data != null)
            {
                while (data.Read())
                {
                    userData.UserID = data.GetUInt32("userID");
                    userData.Username = data.GetString("username");
                    userData.TwitchID = data.GetString("twitchID");
                    userData.CurrentTitle = data.GetInt32("currentTitle");
                    userData.ClearanceLevel = data.GetUInt32("clearanceLevel");
                    userData.IsValid = true;
                    break;
                }
                data.Close();
                return userData;
            }

            return userData;
        }

        public User UserLookup(string userName, string twitchID, bool createNewWhenNotFound = true)
        {
            var data = UnsafeSelect("select * from users where twitchID=\"" + twitchID + "\";");
            var userData = new User();
            bool found = false;

            if (data != null)
            {
                while (data.Read())
                {
                    userData.UserID = data.GetUInt32("userID");
                    userData.Username = data.GetString("username");
                    userData.TwitchID = data.GetString("twitchID");
                    userData.CurrentTitle = data.GetInt32("currentTitle");
                    userData.ClearanceLevel = data.GetUInt32("clearanceLevel");
                    userData.IsValid = true;
                    found = true;
                    break;
                }

                data.Close();
            }

            if(!found)
            {
                if (createNewWhenNotFound)
                {
                    // Create the user as new and return
                    return CreateNewUser(userName, twitchID);
                }
                else
                {
                    userData.IsValid = false;
                    return userData;
                }
            }

            return userData;
        }

        public MySqlDataReader UnsafeSelect(string queryString)
        {
            if (!Connect()) return null;
            try
            {
                //var cmd = new MySqlCommand(queryString, DBConnection);
                DBCommand.CommandText = queryString;
                DBCommand.Connection = DBConnection;
                var reader = DBCommand.ExecuteReader();
                return reader;
            }
            catch(MySqlException e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        public User CreateNewUser(string userName, string twitchID)
        {
            //var cmd = new MySqlCommand("insert into users(username) values(\"" + userName + "\");", DBConnection);
            DBPreparedCommand.CommandText = "insert into users(username, twitchID) values(@name, @twitchID);";
            DBPreparedCommand.Connection = DBConnection;
            DBPreparedCommand.Parameters.AddWithValue("@name", userName);
            DBPreparedCommand.Parameters.AddWithValue("@twitchID", twitchID);
            DBPreparedCommand.Prepare();
            DBPreparedCommand.ExecuteNonQuery();

            var newUser = UserLookup(twitchID);

            if (newUser.IsValid)
            {
                GiveItem(newUser, ItemStack.CreateStackWithItemID(2, 1000));
                GiveItem(newUser, ItemStack.CreateStackWithItemID(3, 1000));
            }

            return UserLookup(twitchID);
        }

        public Item UnsafeItemLookup(uint itemID)
        {
            Item item = new Item();
            var data = UnsafeSelect("select * from items where itemID=" + itemID + ";");

            bool found = false;
            if (data != null)
            {
                while (data.Read())
                {
                    item.ItemID = data.GetUInt32("itemID");
                    item.ItemName = data.GetString("itemName");
                    item.ItemDesc = data.GetString("itemDesc");
                    item.Stackable = data.GetBoolean("stackable");
                    item.DefaultMetaData = data.GetString("metaData");

                    found = true;
                }

                data.Close();
            }

            if (!found) return null; // Item doesn't exist

            return item;
        }

        public uint InventoryLookup(User user, ItemStack itemStack)
        {
            if (itemStack.BaseItem.Stackable)
            {
                var data = UnsafeSelect("select * from inventory where ownerID=" + user.UserID + " AND itemID=" + itemStack.BaseItem.ItemID + " AND metaData=\"" + itemStack.MetaData + "\";");
                if (data != null)
                {
                    while (data.Read())
                    {
                        var id = data.GetUInt32("rwID");
                        data.Close();
                        return id;
                    }
                    data.Close();
                }
            }

            return 0;
        }

        public uint InventoryLookup(User user, ItemStack inItemStack, out ItemStack outItemStack)
        {
            if (inItemStack.BaseItem.Stackable)
            {
                var data = UnsafeSelect("select * from inventory where ownerID=" + user.UserID + " AND itemID=" + inItemStack.BaseItem.ItemID + " AND metaData=\"" + inItemStack.MetaData + "\";");
                if (data != null)
                {
                    while (data.Read())
                    {
                        var id = data.GetUInt32("rwID");
                        var amount = data.GetUInt32("amount");
                        data.Close();
                        outItemStack = ItemStack.CreateStackWithItemID(inItemStack.BaseItem.ItemID, amount);
                        return id;
                    }
                    data.Close();
                }
            }
            outItemStack = null;
            return 0;
        }

        public bool CreateInventoryRecord(User user, ItemStack itemStack)
        {
            try
            {
                DBPreparedCommand.Parameters.Clear();
                DBPreparedCommand.CommandText = "insert into inventory(ownerID, itemID, amount, metaData) values(@owner, @item, @amount, @metaData);";
                DBPreparedCommand.Connection = DBConnection;
                DBPreparedCommand.Parameters.AddWithValue("@owner", user.UserID);
                DBPreparedCommand.Parameters.AddWithValue("@item", itemStack.BaseItem.ItemID);
                DBPreparedCommand.Parameters.AddWithValue("@amount", itemStack.Amount);
                DBPreparedCommand.Parameters.AddWithValue("@metaData", itemStack.MetaData);
                DBPreparedCommand.Prepare();
                DBPreparedCommand.ExecuteNonQuery();

                return true;
            }
            catch(MySqlException e)
            {
                Console.WriteLine(e.Message);

                return false;
            }
        }

        public void PullBackup()
        {
            var process = Process.GetCurrentProcess();
            string fullPath = process.MainModule.FileName;
            int index = fullPath.LastIndexOf('\\');
            fullPath = fullPath.Substring(0, index);
            string mysqlPath = fullPath + @"\Resources\MySQL\mysqldump.exe";

             // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "cmd.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            StringBuilder argumentsBuilder = new StringBuilder();
            argumentsBuilder.Append("/c ").Append(mysqlPath).Append(" -e -u").Append(_databaseInfo.User).Append(" -p")
                .Append(_databaseInfo.Password).Append(" -h").Append(_databaseInfo.ServerIP)
                .Append(" ").Append(_databaseInfo.DatabaseName).Append(" > ").Append(fullPath).Append("\\")
                .Append(_databaseInfo.BackupLocation).Append("\\Backup-")
                .Append(DateTime.Now.ToString().Replace(':', '-').Replace(' ', '_')).Append(".sql");

            startInfo.Arguments = argumentsBuilder.ToString();

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #endregion
    }
}
