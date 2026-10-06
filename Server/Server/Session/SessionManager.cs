using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    internal class SessionManager
    {
        private readonly object _lock = new object();
        private readonly Dictionary<int, ClientSession> _sessions = new Dictionary<int, ClientSession>();

        // 동시 세션 수 상한. 넘으면 세션을 만들지 않고, Listener가 받은 소켓을 닫는다.
        private const int MaxSessionCount = 1000;

        private int _sessionId;
        public static SessionManager Instance { get; } = new SessionManager();

        public int GetBusyScore()
        {
            int count = 0;

            lock (_lock)
            {
                count = _sessions.Count;
            }

            return count / 100;
        }

        public List<ClientSession> GetSessions()
        {
            List<ClientSession> sessions;

            lock (_lock)
            {
                sessions = _sessions.Values.ToList();
            }

            return sessions;
        }

        public ClientSession Generate()
        {
            lock (_lock)
            {
                if (_sessions.Count >= MaxSessionCount)
                    return null;

                int sessionId = ++_sessionId;

                ClientSession session = new ClientSession
                {
                    SessionId = sessionId
                };
                _sessions.Add(sessionId, session);

                Console.WriteLine($"Connected ({_sessions.Count}) Players");

                return session;
            }
        }

        public ClientSession Find(int id)
        {
            lock (_lock)
            {
                _sessions.TryGetValue(id, out ClientSession session);
                return session;
            }
        }

        public void Remove(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session.SessionId);
                Console.WriteLine($"Connected ({_sessions.Count}) Players");
            }
        }
    }
}