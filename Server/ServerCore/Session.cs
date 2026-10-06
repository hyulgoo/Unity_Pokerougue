using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
	public abstract class PacketSession : Session
	{
        public const int HeaderSize = 2;

        // size(2) + packetId(2). 이보다 작은 크기를 적은 패킷은 조립할 수 없다.
        private const int MinPacketSize = 4;

        // [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
        protected PacketSession(Socket socket) : base(socket)
        {
        }

        public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			int processLen = 0;

			while (true)
			{
				// 최소한 헤더는 파싱할 수 있는지 확인
				if (buffer.Count < HeaderSize)
					break;

				// 패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);

                // 크기가 0이면 버퍼가 줄지 않아 이 루프가 끝나지 않는다. 음수를 돌려주면 호출한 쪽이 접속을 끊는다.
                if (dataSize < MinPacketSize)
                    return -1;

                if (buffer.Count < dataSize)
                    break;

                // 여기까지 왔으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

			return processLen;
		}

        protected abstract void OnRecvPacket(ArraySegment<byte> buffer);
	}

	public abstract class Session
	{
		Socket _socket;
		int _disconnected = 0;

		RecvBuffer _recvBuffer = new RecvBuffer(65535);

		object _lock = new object();
		Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        protected Session(Socket socket)
        {
            _socket = socket;
        }

        protected bool IsDisconnected => _disconnected == 1;

        public abstract void OnConnected(EndPoint endPoint);
		public abstract int  OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

        private void Clear()
		{
			lock (_lock)
			{
				_sendQueue.Clear();
				_pendingList.Clear();
			}
		}

		public void Start(Socket socket)
		{
			_socket = socket;

			_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
			_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

			RegisterRecv();
		}

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (_lock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Disconnect()
		{
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			OnDisconnected(_socket.RemoteEndPoint);

			// 상대가 먼저 끊은 소켓은 Shutdown이 예외를 던진다. 수신 완료 콜백에서 불리므로 밖으로 내보내지 않는다.
			try
			{
				_socket.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException)
			{
			}

			_socket.Close();
			Clear();
		}

		#region 네트워크 통신

        private void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;

			try
			{
				bool pending = _socket.SendAsync(_sendArgs);
				if (!pending)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}

        public void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (_lock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						_sendArgs.BufferList = null;
						_pendingList.Clear();

						OnSend(_sendArgs.BytesTransferred);

						if (_sendQueue.Count > 0)
							RegisterSend();
					}
					catch (Exception e)
					{
						Console.WriteLine($"OnSendCompleted Failed {e}");
					}
				}
				else
				{
					Disconnect();
				}
			}
		}

        public void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			_recvBuffer.Clean();
			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
				bool pending = _socket.ReceiveAsync(_recvArgs);
				if (!pending)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}

        public void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
			{
				try
				{
					// Write 커서 이동
					if (!_recvBuffer.OnWrite(args.BytesTransferred))
					{
						Disconnect();
						return;
					}

					// 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
					int processLen = OnRecv(_recvBuffer.ReadSegment);
					if (processLen < 0 || _recvBuffer.DataSize < processLen)
					{
						Disconnect();
						return;
					}

					// Read 커서 이동
					if (!_recvBuffer.OnRead(processLen))
					{
						Disconnect();
						return;
					}

					RegisterRecv();
				}
				catch (Exception e)
				{
					// 수신을 다시 걸지 않으면 이 세션은 패킷을 더 받지 못한 채 남는다. 끊어서 정리한다.
					Console.WriteLine($"OnRecvCompleted Failed {e}");
					Disconnect();
				}
			}
			else
			{
				Disconnect();
			}
		}

		#endregion
	}
}
