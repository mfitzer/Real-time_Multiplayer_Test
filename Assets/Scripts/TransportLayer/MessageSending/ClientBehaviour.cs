using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace MessageSending
{
    public class ClientBehaviour : MonoBehaviour
    {
        private static ClientBehaviour instance;
        public static ClientBehaviour Instance
        {
            get
            {
                if (!instance)
                    instance = FindObjectOfType<ClientBehaviour>();
                return instance;
            }
        }

        public UdpNetworkDriver networkDriver;
        public NetworkConnection connectionToServer; //Connection to the network
        NetworkEndPoint networkEndPoint; //Endpoint configured for connecting to the server

        public bool done; //Indicates when client is done with the server

        NetworkPipeline networkPipeline; //Pipeline used for transporting packets

        Queue<Message> messageQueue; //Holds messages waiting to be sent to the server

        NetworkSettings networkSettings; //Settings for the network connection

        //Indicates if client is connected to the server
        public bool connectedToServer
        {
            get
            {
                if (!connectionToServer.IsCreated) //Connection wasn't created
                {
                    //if (!done) //Not done with the server
                    //Debug.Log("Something went wrong during connect");
                    return false;
                }

                return true;
            }
        }

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
                typeof(ReliableSequencedPipelineStage)
            );

            connectionToServer = default(NetworkConnection); //Setup up default network connection

            //Set up server address
            if (networkSettings.serverIP != "") //Server IP is set
            {
                networkEndPoint = NetworkEndPoint.Parse(networkSettings.serverIP, networkSettings.port);
                Debug.Log("Connecting to server: " + networkSettings.serverIP + " on port: " + networkSettings.port);
            }
            else
            {
                Debug.Log("Connecting to server on LoopbackIpv4");
                networkEndPoint = NetworkEndPoint.LoopbackIpv4;
                networkEndPoint.Port = networkSettings.port;
            }

            connectToServer();
        }

        //Connect to server
        bool connectToServer()
        {
            connectionToServer = networkDriver.Connect(networkEndPoint);

            return connectionToServer.IsCreated; //Indicate if the server connection has been created
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
                    Debug.Log("<color=green>We are now connected to the server</color>");
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
                    Debug.Log("<color=red>Client got disconnected from server</color>");

                    if (!done) //Wasn't supposed to disconnect
                    {
                        Debug.Log("<color=blue>Attempting to reconnect to server.</color>");
                        connectToServer(); //Try to reconnect
                    }

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
            if (connectedToServer)
                messageQueue.Enqueue(message);
        }

        //Sends messages in messageQueue
        void sendMessages()
        {
            if (connectedToServer)
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

                        Debug.Log("Sending message of length: " + msgBytes.Length);
                    }
                }
            }
        }

        #endregion Message Sending

        void Update()
        {
            updateNetworkEvents();

            if (connectedToServer) //Connected to server
            {
                processNetworkEvents(); //Process any new network events
                sendMessages(); //Send any queued messages
            }
        }
    }
}