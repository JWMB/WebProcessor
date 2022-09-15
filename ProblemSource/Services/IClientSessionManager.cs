namespace ProblemSource.Services
{
    public interface IClientSessionManager
    {
        GetOrCreateSessionResult GetOrOpenSession(string? sessionToken);
    }

    public class Session
    {
        public enum SessionStates
        {
            Active,
            Overtaken,
            Purged
        }
        private class CallInfo
        {
            public DateTime Time { get; set; }
            public CallInfo()
            {
                Time = DateTime.Now;
            }
        }
        public SessionStates SessionState { get; set; } //private set
        private List<CallInfo> Calls = new List<CallInfo>();
        public string SessionToken { get; set; }
        //public string ApiKey { get; set; }

        public DateTime StartTime  => Calls[0].Time;
        public DateTime LastActivityTime  => Calls[^1].Time;
        public int NumCalls => Calls.Count;

        public Session()
        {
            MadeCall();
            SessionState = SessionStates.Active;
        }
        public void MadeCall()
        {
            Calls.Add(new CallInfo { });
        }
    }

    public class GetOrCreateSessionResult
    {
        public bool AlreadyExisted { get; set; }
        public Session Session { get; private set; }
        public ErrorTypes Error { get; set; } = ErrorTypes.OK;


        public GetOrCreateSessionResult(Session session)
        {
            Session = session;
        }

        public enum ErrorTypes
        {
            OK,
            SessionNotFound,
            SessionWasPurged,
            SessionWasOvertaken
        }
    }

    public class InMemorySessionManager : IClientSessionManager
    {
        List<Session> _sessions = new List<Session>();
        object _listLock = new object();

        List<Session> Purge_NO_LOCK()
        {
            var timeLimit = DateTime.Now.AddMinutes(-20);
            var toPurge = _sessions.Where(_ => _.SessionState != Session.SessionStates.Purged && _.LastActivityTime < timeLimit).ToList();
            toPurge.ForEach(_ => _.SessionState = Session.SessionStates.Purged);

            timeLimit = timeLimit.AddMinutes(-5);
            var toRemove = _sessions.Where(_ => _.SessionState == Session.SessionStates.Purged && _.LastActivityTime < timeLimit).ToList();
            _sessions.RemoveAll(_ => toRemove.Contains(_));

            return toPurge;
        }

        public GetOrCreateSessionResult GetOrOpenSession(string? sessionToken) //SyncInData sid)
        {
            lock (_listLock)
            {
                Purge_NO_LOCK();
                var error = GetOrCreateSessionResult.ErrorTypes.OK;

                var found = sessionToken == null ? null : _sessions.Find(_ => _.SessionToken == sessionToken);
                if (found != null)
                {
                    if (found.SessionState != Session.SessionStates.Active)
                    {
                        error = found.SessionState == Session.SessionStates.Purged ?
                            GetOrCreateSessionResult.ErrorTypes.SessionWasPurged : GetOrCreateSessionResult.ErrorTypes.SessionWasOvertaken;
                    }
                    else
                    {
                        found.MadeCall();
                    }
                }
                else
                {
                    var otherOngoing = _sessions.Where(_ => /*_.ApiKey == sid.ApiKey &&*/ _.SessionState == Session.SessionStates.Active).ToList();
                    if (otherOngoing.Count > 0)
                    {
                        //TODO: send message to others and await logout, timeout or activity from them.
                        //For now, put those into _overtakenSessions and disallow further activity
                        otherOngoing.ForEach(_ => _.SessionState = Session.SessionStates.Overtaken);
                    }
                }
                var result = new GetOrCreateSessionResult(found == null ? CreateSession_NO_LOCK() : found)
                {
                    AlreadyExisted = found != null,
                    Error = error
                };
                return result;
            }
        }
        private Session CreateSession_NO_LOCK() //SyncInData sid
        {
            var s = new Session() { SessionToken = Guid.NewGuid().ToString(), /*ApiKey = sid.ApiKey*/ };
            this._sessions.Add(s);
            return s;
        }
        private Session CreateSession() //SyncInData sid
        {
            lock (_listLock)
            {
                Purge_NO_LOCK();
                return CreateSession_NO_LOCK();
            }
        }
        private void CloseSession(Session s)
        {
            lock (_listLock)
            {
                _sessions.Remove(s);
            }
        }
    }
}
