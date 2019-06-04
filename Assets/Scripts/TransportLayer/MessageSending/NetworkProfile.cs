using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSending
{
    [CreateAssetMenu(fileName = "New Network Profile", menuName = "Network Profile")]
    public class NetworkProfile : ScriptableObject
    {
        [Tooltip("Port on which the client(s) will be connecting to the server.")]
        public ushort port = 9000;

        [Tooltip("IP address of the server to which client(s) connect.")]
        public string serverIP = "";
    }
}
