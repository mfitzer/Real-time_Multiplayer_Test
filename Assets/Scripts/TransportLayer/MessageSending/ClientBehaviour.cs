using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace MessageSending
{
    public class ClientBehaviour : MonoBehaviour
    {
        public UdpNetworkDriver networkDriver;
        public NetworkConnection connectionToServer; //Connection to the network

        public bool done; //Indicates when client is done with the server

        NetworkPipeline networkPipeline; //Pipeline used for transporting packets

        Queue<Message> messageQueue; //Holds messages waiting to be sent to the server

        NetworkSettings networkSettings; //Settings for the network connection

        void Start()
        {
            networkSettings = NetworkSettings.Instance;

            configure();

            messageQueue = new Queue<Message>();
        }

        #region General Network Operations

        //Configures client to connect to a server
        void configure()
        {
            //Creates a network driver that can track up to 32 packets at a time (32 is the limit)
            //https://github.com/Unity-Technologies/multiplayer/blob/master/com.unity.transport/Documentation/pipelines-usage.md
            networkDriver = new UdpNetworkDriver(new ReliableUtility.Parameters { WindowSize = 32 });

            //This must use the same pipeline(s) as the server
            networkPipeline = networkDriver.CreatePipeline(
                typeof(ReliableSequencedPipelineStage),
                typeof(UnreliableSequencedPipelineStage)
            );

            connectionToServer = default(NetworkConnection); //Setup up default network connection

            //Set up server address
            NetworkEndPoint networkEndpoint;

            if (networkSettings.serverIP != "") //Server IP is set
            {
                networkEndpoint = NetworkEndPoint.Parse(networkSettings.serverIP, networkSettings.port);
                Debug.Log("Connecting to server: " + networkSettings.serverIP + " on port: " + networkSettings.port);
            }
            else
            {
                Debug.Log("Connecting to server on LoopbackIpv4");
                networkEndpoint = NetworkEndPoint.LoopbackIpv4;
                networkEndpoint.Port = networkSettings.port;
            }

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
                }
                else if (networkEvent == NetworkEvent.Type.Data) //Server connection sent data
                {
                    #region Custom Data Processing

                    Debug.Log("Parsing message");

                    MessageParser.parse(stream); //Parse data

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

        #endregion General Network Operations

        #region Message Sending

        //Adds the message to the queue of messages to be sent
        public void sendMessage(Message message)
        {
            messageQueue.Enqueue(message);
        }

        //Sends messages in messageQueue
        void sendMessages()
        {
            //Send each message in messageQueue
            while (messageQueue.Count > 0)
            {
                Message message = messageQueue.Dequeue(); //Get next message
                byte[] msgBytes = message.toBytes(); //Convert message to bytes

                //DataStreamWriter is needed to send data
                //using statement makes sure DataStreamWriter memory is disposed
                using (var writer = new DataStreamWriter(msgBytes.Length, Allocator.Temp))
                {
                    writer.Write(msgBytes); //Write msg byte data

                    //Send msg data to server
                    connectionToServer.Send(networkDriver, networkPipeline, writer);
                }
            }
        }

        #endregion Message Sending

        void Update()
        {
            updateNetworkEvents();

            if (checkServerConnection()) //Connected to server
            {
                processNetworkEvents(); //Process any new network events
                sendMessages(); //Send any queued messages
            }            
        }
    }
}