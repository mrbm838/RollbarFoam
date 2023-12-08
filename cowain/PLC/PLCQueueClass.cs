using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cowain.Comm
{
    public class PLCResult
    {
        public string address = string.Empty;
        public string result = string.Empty;

        public PLCResult(string address, string result)
        {
            this.address = address;
            this.result = result;
        }
    }

    internal class PLCQueueClass
    {
        public static List<PLCResult> listResult = new List<PLCResult>();
        public static List<PLCResult> listWriteResult = new List<PLCResult>();
        public static List<PLCAddress> listRead = new List<PLCAddress>();
        public static ConcurrentQueue<PLCAddress> plcWriteQueue = new ConcurrentQueue<PLCAddress>();
        private static object locker = new object();
        private static object lockerWrite = new object();

        public static List<PLCResult> listResultOther = new List<PLCResult>();
        public static List<PLCResult> listWriteResultOther = new List<PLCResult>();
        public static List<PLCAddress> listReadOther = new List<PLCAddress>();
        public static ConcurrentQueue<PLCAddress> plcWriteQueueOther = new ConcurrentQueue<PLCAddress>();
        private static object lockerOther = new object();
        private static object lockerWriteOther = new object();

        public static void AddToQueue(PLCAddress plcAddress)
        {
            plcWriteQueue.Enqueue(plcAddress);
            foreach (PLCResult item in listWriteResult)
            {
                if (item.address == plcAddress.address)
                {
                    item.result = string.Empty;
                    return;
                }
            }
            listWriteResult.Add(new PLCResult(plcAddress.address, string.Empty));
        }

        public static PLCAddress RemoveFromQueue()
        {
            PLCAddress plcAddress = null;
            if (!plcWriteQueue.IsEmpty)
            {
                plcWriteQueue.TryDequeue(out plcAddress);
            }
            return plcAddress;
        }

        public static void AddResultToList(string address, string result)
        {
            lock (locker)
            {
                foreach (PLCResult item in listResult)
                {
                    if (item.address == address)
                    {
                        item.result = result;
                        return;
                    }
                }
                listResult.Add(new PLCResult(address, string.Empty));
            }
        }

        public static void AddWriteResultToList(string address, string result)
        {
            lock (lockerWrite)
            {
                foreach (PLCResult item in listWriteResult)
                {
                    if (item.address == address)
                    {
                        item.result = result;
                        return;
                    }
                }
                listWriteResult.Add(new PLCResult(address, string.Empty));
            }
        }

        public static string GetResultFromList(string address)
        {
            lock (locker)
            {
                foreach (PLCResult item in listResult)
                {
                    if (item.address == address)
                    {
                        return item.result;
                    }
                }
                return "Fail";
            }
        }

        public static string GetWriteResultFromList(string address)
        {
            lock (lockerWrite)
            {
                foreach (PLCResult item in listWriteResult)
                {
                    if (item.address == address)
                    {
                        return item.result;
                    }
                }
                return "Fail";
            }
        }

        public static void AddToQueueOther(PLCAddress plcAddress)
        {
            plcWriteQueueOther.Enqueue(plcAddress);
            foreach (PLCResult item in listWriteResultOther)
            {
                if (item.address == plcAddress.address)
                {
                    item.result = string.Empty;
                    return;
                }
            }
            listWriteResultOther.Add(new PLCResult(plcAddress.address, string.Empty));
        }

        public static PLCAddress RemoveFromQueueOther()
        {
            PLCAddress plcAddress = null;
            if (!plcWriteQueueOther.IsEmpty)
            {
                plcWriteQueueOther.TryDequeue(out plcAddress);
            }
            return plcAddress;
        }

        public static void AddResultToListOther(string address, string result)
        {
            lock (lockerOther)
            {
                foreach (PLCResult item in listResultOther)
                {
                    if (item.address == address)
                    {
                        item.result = result;
                        return;
                    }
                }
                listResultOther.Add(new PLCResult(address, string.Empty));
            }
        }

        public static void AddWriteResultToListOther(string address, string result)
        {
            lock (lockerWriteOther)
            {
                foreach (PLCResult item in listWriteResultOther)
                {
                    if (item.address == address)
                    {
                        item.result = result;
                        return;
                    }
                }
                listWriteResultOther.Add(new PLCResult(address, string.Empty));
            }
        }

        public static string GetResultFromListOther(string address)
        {
            lock (lockerOther)
            {
                foreach (PLCResult item in listResultOther)
                {
                    if (item.address == address)
                    {
                        return item.result;
                    }
                }
                return "Fail";
            }
        }

        public static string GetWriteResultFromListOther(string address)
        {
            lock (lockerWriteOther)
            {
                foreach (PLCResult item in listWriteResultOther)
                {
                    if (item.address == address)
                    {
                        return item.result;
                    }
                }
                return "Fail";
            }
        }

    }
}
