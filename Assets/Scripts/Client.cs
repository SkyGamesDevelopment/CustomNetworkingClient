using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;

public class Client : MonoBehaviour
{
	#region variables
	public static Client instance;

	public static int dataBufferSize = 4096;
	public TCP tcp;
	public UDP udp;

	private string ip = "192.168.0.10";
	private int port = 7777;
	public int myId = 0;

	private static Dictionary<int, PacketHandler> packetHandlers;
	#endregion

	private delegate void PacketHandler(Packet packet);

	private void Awake()
	{
		if (instance == null)
			instance = this;
		else
			Destroy(this);
	}

	private void Start()
	{
		tcp = new TCP();
		udp = new UDP();

		ConnectToServer();
	}

	private void ConnectToServer()
	{
		InitializeClientData();
		tcp.Connect();
	}

	public class TCP
	{
		public TcpClient socket;
		private NetworkStream stream;
		private Packet receivedData;
		private byte[] receiveBuffer;

		public void Connect()
		{
			socket = new TcpClient
			{
				ReceiveBufferSize = dataBufferSize,
				SendBufferSize = dataBufferSize
			};

			receiveBuffer = new byte[dataBufferSize];
			socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
		}

		private void ConnectCallback(IAsyncResult result)
		{
			socket.EndConnect(result);

			if (!socket.Connected)
			{
				Debug.LogError("Cant connect to server :c");
				return;
			}

			stream = socket.GetStream();
			stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

			receivedData = new Packet();
		}

		public void SendData(Packet packet)
		{
			try
			{
				if (socket != null)
					stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error while sending data to server via TCP: {ex}");
			}
		}

		private void ReceiveCallback(IAsyncResult result)
		{
			try
			{
				int byteLength = stream.EndRead(result);

				if (byteLength <= 0)
				{
					//TODO Disconnect
					return;
				}

				byte[] data = new byte[byteLength];
				Array.Copy(receiveBuffer, data, byteLength);

				receivedData.Reset(HandleData(data));

				stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error receiving TCP data: {ex}");

				//TODO Disconnect
			}
		}

		private bool HandleData(byte[] data)
		{
			int packetLenght = 0;

			receivedData.SetBytes(data);

			if (receivedData.UnreadLength() >= 4)
			{
				packetLenght = receivedData.ReadInt();

				if (packetLenght <= 0)
					return true;
			}

			while (packetLenght > 0 && packetLenght <= receivedData.UnreadLength())
			{
				byte[] packetBytes = receivedData.ReadBytes(packetLenght);

				ThreadManager.ExecuteOnMainThread(() =>
				{
					using (Packet packet = new Packet(packetBytes))
					{
						int packetId = packet.ReadInt();
						packetHandlers[packetId](packet);
					}
				});

				packetLenght = 0;

				if (receivedData.UnreadLength() >= 4)
				{
					packetLenght = receivedData.ReadInt();

					if (packetLenght <= 0)
						return true;
				}
			}

			if (packetLenght <= 1)
				return true;
			else
				return false;
		}
	}

	public class UDP
	{
		public UdpClient socket;
		public IPEndPoint endPoint;

		public UDP()
		{
			endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
		}

		public void Connect(int localPort)
		{
			socket = new UdpClient(localPort);

			socket.Connect(endPoint);
			socket.BeginReceive(ReceiveCallback, null);

			using (Packet packet = new Packet())
			{
				SendData(packet);
			}
		}

		public void SendData(Packet packet)
		{
			try
			{
				packet.InsertInt(instance.myId);

				if (socket != null)
					socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
			}
			catch(Exception ex)
			{
				Debug.LogError($"Error sending data to server via UDP: {ex}");
			}
		}

		private void ReceiveCallback(IAsyncResult result)
		{
			try
			{
				byte[] data = socket.EndReceive(result, ref endPoint);
				socket.BeginReceive(ReceiveCallback, null);

				if (data.Length < 4)
				{
					//TODO disconnect
					return;
				}

				HandleData(data);
			}
			catch(Exception ex)
			{
				//TODO disconnect
			}
		}

		private void HandleData(byte[] data)
		{
			using (Packet packet = new Packet(data))
			{
				int packetLength = packet.ReadInt();
				data = packet.ReadBytes(packetLength);
			}

			ThreadManager.ExecuteOnMainThread(() =>
			{
				using (Packet packet = new Packet(data))
				{
					int packetId = packet.ReadInt();
					packetHandlers[packetId](packet);
				}
			});
		}
	}

	private void InitializeClientData()
	{
		packetHandlers = new Dictionary<int, PacketHandler>()
		{
			{ (int)ServerPackets.TCP_HandShake, ClientHandle.TCP_HandShake },
			{ (int)ServerPackets.TCP_SpawnPlayer, ClientHandle.TCP_SpawnPlayer },
			{ (int)ServerPackets.UDP_PlayerPosition, ClientHandle.UDP_PlayerPosition },
			{ (int)ServerPackets.UDP_PlayerRotation, ClientHandle.UDP_PlayerRotation }
		};

		Debug.Log("Client packets initialized");
	}
}
