using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

// 로컬접속 테스트 클라이언트 
// 서버 => FirstServer
public class FirstTCPClient : MonoBehaviour
{
	private string m_Ip = "localhost";
	private int m_Port = 50003;
	private TcpClient m_Client;
	private Thread m_ThrdClientReceive;




	void Start()
	{
		ConnectToTcpServer();
	}

	void Update()
	{
		SendMyMessage("연결됨");
	}

	void ConnectToTcpServer()
	{
		try
		{
			m_ThrdClientReceive = new Thread(new ThreadStart(ListenForData));
			m_ThrdClientReceive.IsBackground = true;
			m_ThrdClientReceive.Start();
		}
		catch (Exception ex)
		{
			Debug.Log(ex);
		}
	}

	void ListenForData()
	{
		try
		{
			m_Client = new TcpClient(m_Ip, m_Port);
			Byte[] bytes = new Byte[1024];
			while (true)
			{
				if (m_Client.Connected)
				{
					using (NetworkStream stream = m_Client.GetStream())
					{
						int length;

						while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
						{
							var incommingData = new byte[length];
							Array.Copy(bytes, 0, incommingData, 0, length);

							string serverMessage = Encoding.Default.GetString(incommingData);
							Debug.Log(serverMessage); // 받은 값
						}
					}
				}

			}


		}

		catch (SocketException ex)
		{
			Debug.Log(ex);
		}
	}


	void SendMyMessage(string message)
	{
		if (m_Client == null)
		{
			return;
		}

		try
		{
			if (m_Client.Connected)
			{
				NetworkStream stream = m_Client.GetStream();

				if (stream.CanWrite)
				{
					byte[] clientMessageAsByteArray = Encoding.Default.GetBytes(message);
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
				}
			}
		}

		catch (SocketException ex)
		{
			Debug.Log(ex);
		}
	}

	void OnApplicationQuit()
	{
		m_ThrdClientReceive.Abort();

		if (m_Client != null)
		{
			m_Client.Close();
			m_Client = null;
		}
	}

}