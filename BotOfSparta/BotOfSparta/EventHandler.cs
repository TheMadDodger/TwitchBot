//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BotOfSparta
//{
//    public class EventHandling
//    {
//        private Dictionary<string, EventHandler> HandlerList = new Dictionary<string, EventHandler>();
//        private Bot ConnectedBot = null;

//        private static EventHandling Instance = null;

//        public static EventHandling GetInstance()
//        {
//            if (Instance == null) Instance = new EventHandling();
//            return Instance;
//        }

//        public void RegisterEvent(string ev, EventHandler handler)
//        {
//            HandlerList.Add(ev, handler);
//        }

//        public bool AttemptEventExecute(string ev)
//        {
//            EventHandler handler = null;
//            HandlerList.TryGetValue(ev, out handler);

//            if (handler != null)
//                return handler.OnExecuteEventRoot();

//            return false;
//        }

//        public void SetBot(Bot bot)
//        {
//            ConnectedBot = bot;
//        }

//        public Bot GetBot()
//        {
//            return ConnectedBot;
//        }
//    }

//    public class EventHandler
//    {
//        public bool OnExecuteEventRoot()
//        {
//            return OnExecuteEvent();
//        }
//        public virtual bool OnExecuteEvent() { return false; }
//    }

//    public class GoldTreasureEvent : EventHandler
//    {
//        public override bool OnExecuteEvent()
//        {
//            return false;
//        }
//    }

//    public class WatchTimeEvent : EventHandler
//    {
//        public override bool OnExecuteEvent()
//        {
//            return false;
//        }
//    }
//}
