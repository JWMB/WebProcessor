using Azure;
using ProblemSource.Services.Storage;

namespace ProblemSource.Services
{
    public interface IClientSessionManager
    {
        // TODO: if we have >1 server instances, this session concept doesn't work. Sure, ARRAffinity but instances can be recycled
        // Either we use a separate single-instance session server, or a pub/sub service so all instances share (some) sessions info
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

        public IUserGeneratedDataRepositoryProvider? UserRepositories { get; set; }
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
                var foundBySessionToken = string.IsNullOrEmpty(sessionToken) ? null : sessions.FirstOrDefault(_ => _.SessionToken == sessionToken);
                var error = GetOrCreateSessionResult.ErrorTypes.OK;

                if (foundBySessionToken != null)
                {
                    if (foundBySessionToken.SessionState != Session.SessionStates.Active)
                    {
                        error = foundBySessionToken.SessionState == Session.SessionStates.Purged
                            ? GetOrCreateSessionResult.ErrorTypes.SessionWasPurged
                            : GetOrCreateSessionResult.ErrorTypes.SessionWasOvertaken;
                    }
                    else
                    {
                        foundBySessionToken.MadeCall();
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
                var result = new GetOrCreateSessionResult(foundBySessionToken == null ? CreateSession(userId) : foundBySessionToken)
                {
                    AlreadyExisted = foundBySessionToken != null,
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

