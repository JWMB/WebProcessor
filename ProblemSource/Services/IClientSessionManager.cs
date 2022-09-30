namespace ProblemSource.Services
{
    public interface IClientSessionManager
    {
        GetOrCreateSessionResult GetOrOpenSession(string userId, string? sessionToken = null);
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
            public DateTime Time { get; } = DateTime.Now;
        }

        public SessionStates SessionState { get; set; }
        public readonly string SessionToken = Guid.NewGuid().ToString();
        public string UserId { get; }

        private List<CallInfo> calls = new();

        //public DateTime StartTime => Calls.First().Time;
        //public int NumCalls => Calls.Count;
        public DateTime LastActivityTime => calls.Last().Time;

        public Session(string userId)
        {
            UserId = userId;
            MadeCall();
            SessionState = SessionStates.Active;
        }
        public void MadeCall()
        {
            calls.Add(new CallInfo());
        }

        // TODO: put cached data here? (e.g. TrainingDay / Phase stats)
        // or in completely separate services?
    }

    public class GetOrCreateSessionResult
    {
        public bool AlreadyExisted { get; set; }
        public Session Session { get; }
        public ErrorTypes Error { get; set; }

        public enum ErrorTypes
        {
            OK,
            SessionNotFound,
            SessionWasPurged,
            SessionWasOvertaken
        }

        public GetOrCreateSessionResult(Session session)
        {
            Session = session;
        }
    }

    public class InMemorySessionManager : IClientSessionManager
    {
        private object _listLock = new object();
        private List<Session> sessions = new();

        public GetOrCreateSessionResult GetOrOpenSession(string userId, string? sessionToken = null)
        {
            lock (_listLock)
            {
                Purge();
                var found = string.IsNullOrEmpty(sessionToken) ? null : sessions.FirstOrDefault(_ => _.SessionToken == sessionToken);
                var error = GetOrCreateSessionResult.ErrorTypes.OK;

                if (found != null)
                {
                    if (found.SessionState != Session.SessionStates.Active)
                    {
                        error = found.SessionState == Session.SessionStates.Purged
                            ? GetOrCreateSessionResult.ErrorTypes.SessionWasPurged
                            : GetOrCreateSessionResult.ErrorTypes.SessionWasOvertaken;
                    }
                    else
                    {
                        found.MadeCall();
                    }
                }
                else
                {
                    var otherOngoing = sessions.Where(_ => _.UserId == userId && _.SessionState == Session.SessionStates.Active).ToList();
                    if (otherOngoing.Count > 0)
                    {
                        //TODO: send message to others and await logout, timeout or activity from them.
                        //For now, put those into _overtakenSessions and disallow further activity
                        otherOngoing.ForEach(_ => _.SessionState = Session.SessionStates.Overtaken);
                    }
                }
                var result = new GetOrCreateSessionResult(found == null ? CreateSession(userId) : found)
                {
                    AlreadyExisted = found != null,
                    Error = error
                };
                return result;
            }

            List<Session> Purge()
            {
                var timeLimit = DateTime.Now.AddMinutes(-20);
                var toPurge = sessions.Where(_ => _.SessionState != Session.SessionStates.Purged && _.LastActivityTime < timeLimit).ToList();
                toPurge.ForEach(_ => _.SessionState = Session.SessionStates.Purged);

                timeLimit = timeLimit.AddMinutes(-5);
                var toRemove = sessions.Where(_ => _.SessionState == Session.SessionStates.Purged && _.LastActivityTime < timeLimit).ToList();
                sessions.RemoveAll(_ => toRemove.Contains(_));

                return toPurge;
            }

            Session CreateSession(string userId)
            {
                var session = new Session(userId);
                sessions.Add(session);
                return session;
            }
        }
    }
}

