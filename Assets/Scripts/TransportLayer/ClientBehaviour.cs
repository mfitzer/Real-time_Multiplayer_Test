using System.Net;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

public class ClientBehaviour : MonoBehaviour
{
    public UdpNetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool m_Done;

    NetworkPipeline networkPipeline;

    void Start ()
	{
        m_Driver = new UdpNetworkDriver(new ReliableUtility.Parameters { WindowSize = 32 });
        m_Connection = default(NetworkConnection);

        NetworkEndPoint networkEndpoint = NetworkEndPoint.LoopbackIpv4;
        networkEndpoint.Port = 9000;

        //This must use the same pipeline(s) as the server
        networkPipeline = m_Driver.CreatePipeline(
            typeof(ReliableSequencedPipelineStage),
            typeof(UnreliableSequencedPipelineStage
        ));

        m_Connection = m_Driver.Connect(networkEndpoint);
    }
    
    public void OnDestroy()
    {
        m_Driver.Dispose();
    }
    
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!m_Done)
                Debug.Log("Something went wrong during connect");
            return;
        }
        
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != 
               NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server");
                
                var value = 1;
                using (var writer = new DataStreamWriter(4, Allocator.Temp))
                {
                    writer.Write(value);
                    m_Connection.Send(m_Driver, networkPipeline, writer);
                }
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                uint value = stream.ReadUInt(ref readerCtx);
                Debug.Log("Got the value = " + value + " back from the server");
                m_Done = true;
                m_Connection.Disconnect(m_Driver);
                m_Connection = default(NetworkConnection);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
        }
    }
}