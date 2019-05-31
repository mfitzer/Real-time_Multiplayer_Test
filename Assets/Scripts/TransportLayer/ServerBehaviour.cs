using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.Assertions;
using Unity.Networking.Transport.Utilities;

namespace TransportLayerTest
{
    public class ServerBehaviour : MonoBehaviour
    {
        public UdpNetworkDriver networkDriver;
        private NativeList<NetworkConnection> networkConnections; //Holds a list of active network connections

        NetworkPipeline networkPipeline; //Pipeline used for transporting packets

        //Size of packet
        const int packetSize = 4; //4 is default in example

        void Start()
        {
            configure();
        }

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

            //Set up network endpoint to accept any Ipv4 connections on port 9000
            NetworkEndPoint networkEndpoint = NetworkEndPoint.AnyIpv4;
            networkEndpoint.Port = 9000;

            //Binds the network driver to a specific network address and port
            if (networkDriver.Bind(networkEndpoint) != 0)
                Debug.Log("Failed to bind to port 9000");
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
                        //Tracks where in the data stream you are and how much you've read
                        var readerContext = default(DataStreamReader.Context);

                        #region Custom Data Processing

                        //Attempt to read uint from stream
                        uint number = stream.ReadUInt(ref readerContext);

                        Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                        number += 2;

                        //DataStreamWriter is needed to send data
                        //using statement makes sure DataStreamWriter memory is disposed
                        using (var writer = new DataStreamWriter(packetSize, Allocator.Temp))
                        {
                            writer.Write(number); //Write response data
                            networkDriver.Send(networkPipeline, networkConnections[i], writer); //Send response data to client
                        }

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

        void Update()
        {
            updateNetworkEvents();

            cleanupConnections();

            acceptNewConnections();

            processNetworkEvents();
        }
    }
}