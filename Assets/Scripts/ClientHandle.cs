using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
	#region TCP handle
	public static void TCP_HandShake(Packet packet)
	{
		try
		{
			int myId = packet.ReadInt();

			Debug.Log("TCP connection with server established");
			Client.instance.myId = myId;

			ClientSend.TCP_HandShakeReturn();

			Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
		}
		catch(Exception ex)
		{
			Debug.LogError("OPS! " + ex);
		}
	}

	public static void TCP_SpawnPlayer(Packet packet)
	{
		try
		{
			int id = packet.ReadInt();
			Vector3 position = packet.ReadVector3();
			Quaternion rotation = packet.ReadQuaternion();

			GameManager.instance.SpawnPlayer(id, position, rotation);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error unpacking packet: {ex}");
		}
	}
	#endregion

	#region UDP handle
	public static void UDP_PlayerPosition(Packet packet)
	{
		try
		{
			int id = packet.ReadInt();
			Vector3 position = packet.ReadVector3();

			GameManager.players[id].transform.position = position;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error unpacking packet: {ex}");
		}
	}

	public static void UDP_PlayerRotation(Packet packet)
	{
		try
		{
			int id = packet.ReadInt();
			Quaternion rotation = packet.ReadQuaternion();

			GameManager.players[id].transform.rotation = rotation;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error unpacking packet: {ex}");
		}
	}
	#endregion
}
