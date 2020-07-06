using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
	private static void SendTCPData(Packet packet)
	{
		packet.WriteLength();
		Client.instance.tcp.SendData(packet);
	}

	private static void SendUDPData(Packet packet)
	{
		packet.WriteLength();
		Client.instance.udp.SendData(packet);
	}

	#region TCP packets
	public static void TCP_HandShakeReturn()
	{
		using (Packet packet = new Packet((int)ClientPackets.TCP_HandShakeReturn))
		{
			packet.Write(Client.instance.myId);

			SendTCPData(packet);
		}
	}
	#endregion

	#region UDP packets
	public static void UDP_PlayerInput(bool[] inputs)
	{
		using (Packet packet = new Packet((int)ClientPackets.UDP_PlayerInput))
		{
			packet.Write(inputs.Length);

			foreach (bool x in inputs)
			{
				packet.Write(x);
			}

			packet.Write(GameManager.players[Client.instance.myId].transform.rotation);

			SendUDPData(packet);
		}
	}
	#endregion
}
