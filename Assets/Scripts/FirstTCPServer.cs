using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

// 통신테스트용 서버 

public class FirstTCPServer : MonoBehaviour
{
    // 네트워크 변수
    private int m_Port = 50003;
    private TcpListener m_TcpListener;
    private List<TcpClient> m_Clients = new List<TcpClient>(new TcpClient[0]);
    private Thread m_ThrdtcpListener;
    private TcpClient m_Client;

    // 연결확인용 변수 
    private Text myText;
    private static string myResult;
    private static int myCnt = 1;

    void Start()
    {
        myResult = "(준비중)";
        myText = GameObject.Find("Text").GetComponent<Text>();

        print(LocalIPAddress()); // 로컬 IP주소 확인 

        InitServer();
    }

    void InitServer() // (1) 연결설정, 시작
    {
        m_ThrdtcpListener = new Thread(new ThreadStart(ListenForIncommingRequests));
        m_ThrdtcpListener.IsBackground = true;
        m_ThrdtcpListener.Start();

    }

    void Update()
    {
        // 메시지 보내기는 여기서 계속 반복되고,
        // 메시지 받기는 리스너(귀, 아래 line83. ListenerWorker)가 열려있어,
        // 이 둘이 동시에 켜져있는 상태가 지속됨 

        for (int i = 0; i < m_Clients.Count; i++)
        {
            // (5) 연결되어있지 않다면 클라이언트 목록에서 빼고
            if (!m_Clients[i].Connected)
                m_Clients.RemoveAt(i);

            // 연결되어 있으면 클라이언트에게 메시지를 보냄 
            else
                SendMessage(m_Clients[i], myCnt++.ToString() + ". 안녕?"); // 보내는 값
        }
        myText.text = "클라이언트가 보낸값: " + myResult;
    }

    void OnApplicationQuit()
    {
        // 어플리케이션이 끝나면 연결 해제

        m_ThrdtcpListener.Abort();

        if (m_TcpListener != null)
        {
            m_TcpListener.Stop();
            m_TcpListener = null;
        }
    }

    void ListenForIncommingRequests()
    {
        // (2) 리스너 생성 (모든IP, 위에서 지정한 포트번호 연결받음)
        m_TcpListener = new TcpListener(IPAddress.Any, m_Port); 

        m_TcpListener.Start();
        ThreadPool.QueueUserWorkItem(ListenerWorker, null);
    }

    void ListenerWorker(object token)
    {
        while (m_TcpListener != null)
        {
            // (3) 연결요청하는 클라이언트 받음 
            m_Client = m_TcpListener.AcceptTcpClient();
            m_Clients.Add(m_Client); // 받은 클라이언트 추가 
            ThreadPool.QueueUserWorkItem(HandleClientWorker, m_Client);
        }
    }

    void HandleClientWorker(object token)
    {
        Byte[] bytes = new Byte[1024];
        using (var client = token as TcpClient)
        using (var stream = client.GetStream()) // (4) 클라이언트가 보낸 메시지 받음
        {
            int length;

            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                var incommingData = new byte[length];
                Array.Copy(bytes, 0, incommingData, 0, length);
                string clientMessage = Encoding.Default.GetString(incommingData);
                Debug.Log(clientMessage); // 받은 자료
                myResult = clientMessage;
            }

            if (m_Client == null)
            {
                return;
            }
        }
    }

    void SendMessage(object token, string message) // (6) 메시지 보내기 
    {
        if (m_Client == null)            
            return;

        else
            Debug.Log(m_Clients.Count);

        var client = token as TcpClient;
        {
            try
            {
                NetworkStream stream = client.GetStream();
                if (stream.CanWrite)
                {
                    byte[] serverMessageAsByteArray = Encoding.Default.GetBytes(message);
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                }
            }

            catch (SocketException ex)
            {
                Debug.Log(ex);
                return;
            }
        }
    }   

    public static string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }     
}
