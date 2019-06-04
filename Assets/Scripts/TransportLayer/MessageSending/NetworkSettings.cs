using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSending
{
    public class NetworkSettings : MonoBehaviour
    {
        private static NetworkSettings instance;
        public static NetworkSettings Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<NetworkSettings>();
                return instance;
            }
        }

        [SerializeField]
        private NetworkProfile networkProfile;

        /// <summary>
        /// IP address of the server to which client(s) connect.
        /// </summary>
        public string serverIP
        {
            get
            {
                if (networkProfile) //networkProfile is not null
                    return networkProfile.serverIP;

                return "";
            }
        }

        /// <summary>
        /// Port on which the client(s) will be connecting to the server.
        /// </summary>
        public ushort port
        {
            get
            {
                if (networkProfile) //networkProfile is not null
                    return networkProfile.port;

                return 9000; //Default to 9000
            }
        }
    }
}
