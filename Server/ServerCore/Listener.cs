using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
	public class Listener
	{
        private Socket _listenSocket;
        private Func<Session> _sessionFactory;

        public Listener(Func<Session> sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public Listener()
        {
        }

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
		{
			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_sessionFactory = sessionFactory;

			// 문지기 교육
			_listenSocket.Bind(endPoint);

			// 영업 시작
			// backlog : 최대 대기수
			_listenSocket.Listen(backlog);

			for (int i = 0; i < register; i++)
			{
				SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
				RegisterAccept(args);
			}
		}

        private void RegisterAccept(SocketAsyncEventArgs args)
		{
			args.AcceptSocket = null;

			try
			{
				bool pending = _listenSocket.AcceptAsync(args);
				if (!pending)
					OnAcceptCompleted(null, args);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
		{
			try
			{
				if (args.SocketError == SocketError.Success)
				{
					Session session = _sessionFactory.Invoke();
					if (session == null)
					{
						// 세션을 더 만들 수 없다(세션 수 상한). 받은 소켓을 닫는다.
						args.AcceptSocket.Close();
					}
					else
					{
						// Start가 첫 수신을 처리하다 접속을 끊을 수 있어 주소를 먼저 읽어 둔다.
						EndPoint remoteEndPoint = args.AcceptSocket.RemoteEndPoint;
						session.Start(args.AcceptSocket);
						session.OnConnected(remoteEndPoint);
					}
				}
				else
					Console.WriteLine(args.SocketError.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			RegisterAccept(args);
		}
	}
}
