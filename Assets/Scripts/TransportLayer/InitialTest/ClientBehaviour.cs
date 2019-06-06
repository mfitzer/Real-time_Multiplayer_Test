using System.Net;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace TransportLayerTest
{
    public class ClientBehaviour : MonoBehaviour
    {
        public UdpNetworkDriver networkDriver;
        public NetworkConnection connectionToServer; //Connection to the network
        public bool done; //Indicates when client is done with the server

        NetworkPipeline networkPipeline; //Pipeline used for transporting packets

        void Start()
        {
            configure();
        }

        //Configures client to connect to a server
        void configure()
        {
            //Creates a network driver that can track up to 32 packets at a time (32 is the limit)
            //https://github.com/Unity-Technologies/multiplayer/blob/master/com.unity.transport/Documentation/pipelines-usage.md
            networkDriver = new UdpNetworkDriver(new ReliableUtility.Parameters { WindowSize = 32 });

            //This must use the same pipeline(s) as the server
            networkPipeline = networkDriver.CreatePipeline(
                typeof(ReliableSequencedPipelineStage)
            );

            connectionToServer = default(NetworkConnection); //Setup up default network connection

            //Set up server address
            NetworkEndPoint networkEndpoint = NetworkEndPoint.LoopbackIpv4;
            networkEndpoint.Port = 9000;

            //Connect to server
            connectionToServer = networkDriver.Connect(networkEndpoint);
        }

        public void OnDestroy()
        {
            //Disposes unmanaged memory
            networkDriver.Dispose();
        }

        //Updates network events
        void updateNetworkEvents()
        {
            //Complete C# JobHandle to ensure network event updates can be processed
            networkDriver.ScheduleUpdate().Complete();
        }

        //Check if client is connected to the server
        bool checkServerConnection()
        {
            if (!connectionToServer.IsCreated) //Connection wasn't created
            {
                if (!done) //Not done with the server
                    Debug.Log("Something went wrong during connect");
                return false;
            }

            return true;
        }

        void processNetworkEvents()
        {
            DataStreamReader stream; //Used for reading data from data network events

            //Get network events for the connection
            NetworkEvent.Type networkEvent;
            while ((networkEvent = connectionToServer.PopEvent(networkDriver, out stream)) !=
                   NetworkEvent.Type.Empty)
            {
                if (networkEvent == NetworkEvent.Type.Connect) //Connected to server
                {
                    Debug.Log("We are now connected to the server");

                    #region Sending Custom Data

                    var value = 1; //Value being sent to the server
                    int dataSize = 4; //Size of data being sent in bytes

                    //DataStreamWriter is needed to send data
                    //using statement makes sure DataStreamWriter memory is disposed
                    using (var writer = new DataStreamWriter(dataSize, Allocator.Temp))
                    {
                        writer.Write(value); //Write response data
                        connectionToServer.Send(networkDriver, networkPipeline, writer); //Send response data to server
                    }

                    #endregion Sending Custom Data
                }
                else if (networkEvent == NetworkEvent.Type.Data) //Server connection sent data
                {
                    //Tracks where in the data stream you are and how much you've read
                    var readerContext = default(DataStreamReader.Context);

                    #region Custom Data Processing

                    //Attempt to read uint from stream
                    uint value = stream.ReadUInt(ref readerContext);

                    Debug.Log("Got the value = " + value + " back from the server");

                    done = true; //Set flag to indicate client is done with server

                    //Disconnect from server
                    connectionToServer.Disconnect(networkDriver);

                    //Reset connection to default to avoid stale reference
                    connectionToServer = default(NetworkConnection);

                    #endregion Custom Data Processing
                }
                else if (networkEvent == NetworkEvent.Type.Disconnect) //Disconnected from server
                {
                    Debug.Log("Client got disconnected from server");

                    //Reset connection to default to avoid stale reference
                    connectionToServer = default(NetworkConnection);
                }
            }
        }

        void Update()
        {
            updateNetworkEvents();

            if (checkServerConnection()) //Connected to server
            {
                processNetworkEvents();
            }            
        }
    }
}