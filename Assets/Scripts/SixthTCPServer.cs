using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SixthTCPServer : MonoBehaviour
{
    // 통신 변수
    private int m_Port = 50003;
    private TcpListener m_TcpListener;
    private List<TcpClient> m_Clients = new List<TcpClient>(new TcpClient[0]);
    private Thread m_ThrdtcpListener;
    private TcpClient m_Client;

    private string myMessage; // 아이폰에서 받은 메시지 여기에 받음 

    public SkinnedMeshRenderer faceMeshRenderer; // 페이스트래킹 데이터 받아서 적용할 캐릭터의 얼굴

    [HideInInspector]
    public int characterIndex;


    void Start()
    {
        int characterIndex = PlayerPrefs.GetInt("selectedCharacter");
        print(LocalIPAddress()); // 로컬 IP주소 확인 
        m_ThrdtcpListener = new Thread(new ThreadStart(ListenForIncommingRequests));
        m_ThrdtcpListener.IsBackground = true;
        m_ThrdtcpListener.Start(); // 연결 과정시작 

        if (m_Client.Connected)
        {
            SendMessage(m_Client, characterIndex.ToString());
        }
    }

    void Update()
    {
        for (int i = 0; i < m_Clients.Count; i++) 
        {
            print(i);
            if (!m_Clients[i].Connected) // 클라이언트 목록에는 있는데 연결상태가 아닐경우
                m_Clients.RemoveAt(i); // 목록에서 제거

            else
                //SendMessage(m_Clients[i], characterIndex.ToString());
                StartCoroutine(OpenFaceData(myMessage)); // 아이폰에서 받은 데이터 (사용할 형태로)포장풀기
        }

    }



    IEnumerator OpenFaceData(string msg) {
        int i = 0, j = 0; // 문자열 순회에 사용할 변수 
        var tmp = ""; // 임시로 사용할 문자열변수 
        string[] result; // 받은 데이터 분리할 때 받아줄 배열변수 

        if(msg != null) { // 들어온 메시지가 있다면 실행 
            while ((i = msg.IndexOf('/', i)) != -1) // '/' 는 데이터의 시작을 구분하는 문자로 사용함 
                                                    // 데이터가 몇 묶음 왔는지 확인하며 순회함 
            {
                tmp = msg.Substring(i); // 메시지 중 '/'가 있는 (첫)위치에서 메시지의 끝까지 
                                        // 예) abcd/efg/hijk 가 메시지라면, => /efg/hijk

                j = tmp.IndexOf('&'); // '&' 는 데이터의 끝을 구분하는 문자로 사용함

                if(j > 0) { // 들어오다가 끊어진 데이터가 있어서, '/' 와 '&' 값이 세트로 있는지 확인하는 과정 

                    tmp = msg.Substring(i+1, j-1); // '/' 와 '&' 는 제외한 그 앞과 뒤의 메시지만 추출 

                    string[] stringSeparators = new string[] {"|"}; // 메시지는 '|' 로 구분되어 있음
                                                                    // |key1|value1|key2|value2|...
                    result = tmp.Split(stringSeparators, StringSplitOptions.None); 

                    yield return StartCoroutine(ApplyFaceData(result)); // 데이터를 캐릭터 표정에 적용하기
                    
                } 

                i++;
            }
        } 

        yield return null;
    }

    IEnumerator ApplyFaceData(string [] data) {

        ResetBlendShape();
        if(data[0] == "0" && data.Length == 15) { // 표정데이터 
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[1]), float.Parse(data[2])); // 입
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[3]), float.Parse(data[4])); // 눈 Left
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[5]), float.Parse(data[6])); // 눈 Right
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[7]), float.Parse(data[8])); // 눈동자 Up
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[9]), float.Parse(data[10])); // 눈동자 Down 
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[11]), float.Parse(data[12])); // 눈동자 Left
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[13]), float.Parse(data[14])); // 눈동자 Right
        } else if (data[0] == "1" && data.Length == 3) { // 버튼데이터 
            faceMeshRenderer.SetBlendShapeWeight(Int32.Parse(data[1]), float.Parse(data[2]));
        } else {
            Debug.Log(data.Length);
        }

        yield return null;
    }

    private void ResetBlendShape()
    {
        for(var i =0; i< faceMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            faceMeshRenderer.SetBlendShapeWeight(i, 0f);
        }
    }

    void OnApplicationQuit()
    {
        m_ThrdtcpListener.Abort();

        if (m_TcpListener != null)
        {
            m_TcpListener.Stop();
            m_TcpListener = null;
        }
    }

    void ListenForIncommingRequests() 
    {
        m_TcpListener = new TcpListener(IPAddress.Any, m_Port); 
        // 모든 주소에 대해, 위에서 설정한 포트번호에 한하여 연결함 

        m_TcpListener.Start();
        ThreadPool.QueueUserWorkItem(ListenerWorker, null);
    }

    void ListenerWorker(object token)
    {
        while (m_TcpListener != null) // 연결된 접속이 있다면 
        {
            m_Client = m_TcpListener.AcceptTcpClient(); // 받아서 변수로 받고
            m_Clients.Add(m_Client); // 클라이언트 목록에 추가해줌 
            ThreadPool.QueueUserWorkItem(HandleClientWorker, m_Client); 
        }
    }

    void HandleClientWorker(object token) // 연결된 클라이언트와 할 작업을 실행
    {
        Byte[] bytes = new Byte[1024];
        using (var client = token as TcpClient)
        using (var stream = client.GetStream())
        {
            int length;

            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) // 스트림에 들어온 데이터가 있다면(아이폰에서 보낸 데이터가 있다면)
            {
                var incommingData = new byte[length]; 
                Array.Copy(bytes, 0, incommingData, 0, length); // 보낸 데이터를 변수에 받고
                string clientMessage = Encoding.Default.GetString(incommingData); // 읽을 수 있는 형태로 변환후
                myMessage = clientMessage; // 사용할 변수에 저장함 
            }

            if (m_Client == null) // 연결된 클라이언트가 없다면 빠져나감 
            {
                return;
            }
        }
    }

    void SendMessage(object token, string message)
    {
        if (m_Client == null)            
            return;
            
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

    private void OnDisable()
    {
        StopCoroutine("OpenFaceData");
        StopCoroutine("ApplyFaceData");
    }
}