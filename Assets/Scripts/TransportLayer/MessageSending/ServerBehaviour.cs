using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.Assertions;
using Unity.Networking.Transport.Utilities;

namespace MessageSending
{
    public class ServerBehaviour : MonoBehaviour
    {
        public UdpNetworkDriver networkDriver;
        private NativeList<NetworkConnection> networkConnections; //Holds a list of active network connections

        NetworkPipeline networkPipeline; //Pipeline used for transporting packets

        Queue<Message> messageQueue; //Holds messages waiting to be sent to the clients

        NetworkSettings networkSettings; //Settings for the network connection

        /// <summary>
        /// Indicates if the server has any clients connected to it
        /// </summary>
        public bool hasConnections
        {
            get
            {
                return networkConnections.Length > 0;
            }
        }

        void Start()
        {
            networkSettings = NetworkSettings.Instance;

            configure();

            messageQueue = new Queue<Message>();
        }

        #region General Network Operations

        //Configures server to connect to clients
        void configure()
        {
            //Creates a network driver that can track up to 32 packets at a time (32 is the limit)
            //https://github.com/Unity-Technologies/multiplayer/blob/master/com.unity.transport/Documentation/pipelines-usage.md
            networkDriver = new UdpNetworkDriver(new ReliableUtility.Parameters { WindowSize = 32 });

            //This must use the same pipeline(s) as the client(s)
            networkPipeline = networkDriver.CreatePipeline(
                typeof(ReliableSequencedPipelineStage),
                typeof(UnreliableSequencedPipelineStage)
            );

            //Set up network endpoint to accept any Ipv4 connections on port networkSettings.port
            NetworkEndPoint networkEndpoint = NetworkEndPoint.AnyIpv4;
            networkEndpoint.Port = networkSettings.port;

            //Binds the network driver to a specific network address and port
            if (networkDriver.Bind(networkEndpoint) != 0)
                Debug.Log("Failed to bind to port " + networkSettings.port);
            else //Successfully bound to port 9000
                networkDriver.Listen(); //Start listening for incoming connections

            //Create list that can hold up to 16 connections
            networkConnections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        }

        public void OnDestroy()
        {
            //Disposes unmanaged memory
            networkDriver.Dispose();
            networkConnections.Dispose();
        }

        //Updates network events
        void updateNetworkEvents()
        {
            //Complete C# JobHandle to ensure network event updates can be processed
            networkDriver.ScheduleUpdate().Complete();
        }

        //Clean up old, stale connections
        void cleanupConnections()
        {
            for (int i = 0; i < networkConnections.Length; i++)
            {
                if (!networkConnections[i].IsCreated) //Network connection is not created
                {
                    networkConnections.RemoveAtSwapBack(i);
                    --i;
                }
            }
        }

        //Accepts new network connections
        void acceptNewConnections()
        {
            NetworkConnection newConnection;
            while ((newConnection = networkDriver.Accept()) != default(NetworkConnection))
            {
                networkConnections.Add(newConnection); //Add new connection to active connections
                Debug.Log("Accepted a connection");
            }
        }

        //Process any 
        void processNetworkEvents()
        {
            DataStreamReader stream; //Used for reading data from data network events

            for (int i = 0; i < networkConnections.Length; i++) //For each active connection
            {
                if (!networkConnections[i].IsCreated)
                    Assert.IsTrue(true);

                //Get network events for the connection
                NetworkEvent.Type networkEvent;
                while ((networkEvent = networkDriver.PopEventForConnection(networkConnections[i], out stream)) !=
                       NetworkEvent.Type.Empty)
                {
                    if (networkEvent == NetworkEvent.Type.Data) //Connection sent data
                    {
                        #region Custom Data Processing

                        MessageParser.parse(stream); //Parse data

                        #endregion Custom Data Processing
                    }
                    else if (networkEvent == NetworkEvent.Type.Disconnect) //Connection disconnected
                    {
                        Debug.Log("Client disconnected from server");
                        //This ensures the connection will be cleaned up in cleanupConnections()
                        networkConnections[i] = default(NetworkConnection);
                    }
                }
            }
        }

        #endregion General Network Operations

        #region Message Sending

        /// <summary>
        /// Adds the message to the queue of messages to be sent
        /// </summary>
        public void sendMessage(Message message)
        {
            if (hasConnections) //Server has clients to send the message to
            {
                messageQueue.Enqueue(message);
            }
        }

        //Sends messages in messageQueue
        void sendMessages()
        {
            //Send any queued message to each connected client
            for (int i = 0; i < networkConnections.Length; i++)
            {
                NetworkConnection connectionToClient = networkConnections[i];

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

                        //Send msg data to client
                        connectionToClient.Send(networkDriver, networkPipeline, writer);
                    }
                }
            }
        }

        #endregion Message Sending

        void Update()
        {
            updateNetworkEvents();

            cleanupConnections();

            acceptNewConnections();

            processNetworkEvents();

            sendMessages();
        }
    }
}