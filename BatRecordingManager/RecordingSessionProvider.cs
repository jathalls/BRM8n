using DataVirtualizationLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatRecordingManager
{
    class RecordingSessionProvider : IItemsProvider<RecordingSession>
    {
        private int _count;
        private string _sortColumn = null;


        public RecordingSessionProvider()
        {
            _count = DBAccess.GetRecordingSessionListCount();

        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return (_count);
        }

        public void RefreshCount()
        {
            _count = DBAccess.getRecordingSessionCount();
        }

        public IList<RecordingSession> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("Sess FetchRange: " + startIndex + ", " + count);
            List<RecordingSession> sessionList = new List<RecordingSession>();
            var page = DBAccess.GetPagedRecordingSessionList(count, startIndex, _sortColumn);
            if (page != null)
            {
                sessionList.AddRange(page.ToList());
            }
            return (sessionList);
        }

        public RecordingSession Default()
        {
            
                return new RecordingSession();
            
        }

        public void setSortColumn(string column)
        {
            _sortColumn = column;
        }
    }
    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    class RecordingSessionDataProvider : IItemsProvider<RecordingSessionData>
    {
        private  int _count;
        private string _sortColumn = null;

        public RecordingSessionDataProvider()
        {
            _count = DBAccess.GetRecordingSessionDataCount();
            
        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return (_count);
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingSessionDataCount();
        }

        public IList<RecordingSessionData> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("RSD FetchRange: " + startIndex + ", " + count);
            List<RecordingSessionData> sessionList = new List<RecordingSessionData>();
            var page = DBAccess.GetPagedRecordingSessionDataList(count, startIndex, _sortColumn);
            if (page != null)
            {
                sessionList.AddRange(page.ToList());
            }
            return (sessionList);
        }

        public RecordingSessionData Default()
        {
            
                return new RecordingSessionData();
            
        }

        public void setSortColumn(string column)
        {
            _sortColumn = column;
        }
    }

    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    class BatSessionRecordingDataProvider : IItemsProvider<BatSessionRecordingData>
    {
        private readonly int _count;
        private string _sortColumn = null;
        private List<int> _batIdList;
        private List<int> _sessionIdList;

        public BatSessionRecordingDataProvider(List<int> batIdList,List<int> sessionIdList,int Count=-1)
        {
            //_count = DBAccess.GetBatSessionRecordingDataCount(batIdList, sessionIdList);
            
            _batIdList = batIdList;
            _sessionIdList = sessionIdList;
            if (Count >= 0) _count = Count;
            Trace.WriteLine("new Provider for BSRD " + _count + "recordings in total");
        }

        public int FetchCount()
        {
            Trace.WriteLine("BSRDP FetchCount");
            return (_count);
        }

        public void RefreshCount()
        {

        }

        public IList<BatSessionRecordingData> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("BSRD FetchRange: " + startIndex + ", " + count);
            List<BatSessionRecordingData> sessionList = new List<BatSessionRecordingData>();
            try
            {
                
                var page = DBAccess.GetPagedBatSessionRecordingData(_batIdList, _sessionIdList, startIndex, count);
                
                
                if (page != null)
                {
                   
                    var list = page.ToList();
                    
                    sessionList.AddRange(list);
                    
                }
                
            }catch(Exception ex)
            {
                Debug.WriteLine("BSRDP Error fetching BatSessionrecordingData " + startIndex + " to " + (startIndex + count));
            }
            return (sessionList);
        }

        public BatSessionRecordingData Default()
        {
            
                return new BatSessionRecordingData();
            
        }

        public void setSortColumn(string column)
        {
            _sortColumn = column;
        }
    }

    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    class RecordingProvider : IItemsProvider<Recording>
    {
        private  int _count;
        private string _sortColumn = null;

        public RecordingProvider()
        {
            _count = DBAccess.GetRecordingListCount();
            
        }

        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            return (_count);
        }

        public void RefreshCount()
        {
            _count = DBAccess.GetRecordingListCount();
        }

        public IList<Recording> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("Rec FetchRange: " + startIndex + ", " + count);
            List<Recording> recordingList = new List<Recording>();
            var page = DBAccess.GetPagedRecordingList(count, startIndex, _sortColumn);
            if (page != null)
            {
                recordingList.AddRange(page.ToList());
            }
            return (recordingList);
        }

        public Recording Default()
        {
            
                return new Recording();
            
        }

        public void setSortColumn(string column)
        {
            _sortColumn = column;
        }
    }

}
