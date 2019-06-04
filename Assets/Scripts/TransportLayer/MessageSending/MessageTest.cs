using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSending;

public class MessageTest : MonoBehaviour
{
    public ServerBehaviour server;
    public Transform transformToUpdate;

    // Start is called before the first frame update
    void Start()
    {
        TransformMessage transformMsg = new TransformMessage(transformToUpdate);
        server.sendMessage(transformMsg);

        DisableMessage enableMsg = new DisableMessage();
        enableMsg.objToEnable = "Cube";
        server.sendMessage(enableMsg);
    }
}
