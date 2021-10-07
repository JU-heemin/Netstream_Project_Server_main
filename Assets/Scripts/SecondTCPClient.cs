using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

// 아이폰접속 테스트 클라이언트 
// 서버 => FirstServer

public class SecondTCPClient : MonoBehaviour
{
 	private string m_Ip = "192.168.0.5";
	private int m_Port = 50005; 
    // 서버포트는 50003, 
    // 포트포워딩해서 50005 으로 사용중, (설정문서 참조)
    // 포트포워딩 안할경우, 서버와 동일하게 50003 으로 사용하기 
	private TcpClient m_Client;
	private Thread m_ThrdClientReceive;

	void Start()
	{
		ConnectToTcpServer();
	}

	void Update()
	{
		SendMyMessage("아이폰 연결됨");
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
							//Debug.Log(serverMessage); // 받은 값
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