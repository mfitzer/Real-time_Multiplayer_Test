# Real-time Multiplayer Test
This project is a sample project utilizing Unity game engine's new [Real-time Multiplayer](https://github.com/Unity-Technologies/multiplayer) solution. 

## Transport Layer Test
The *TransportLayerTest* scene found in `Assets/Scenes` uses Unity's new [transport layer](https://github.com/Unity-Technologies/multiplayer/tree/master/com.unity.transport).

The scripts found in `Assets/Scripts/TransportLayer` are updated versions of scripts found in the [transport layer documentation](https://github.com/Unity-Technologies/multiplayer/tree/master/com.unity.transport/Documentation~/samples). The documentation is out of date so the scripts needed to be updated to work with the current APIs. I have also done some reorganization and added comments to the scripts based on the transport layer documentation under the [Workflow: Creating a minimal client and server](https://github.com/Unity-Technologies/multiplayer/blob/master/com.unity.transport/Documentation~/workflow-client-server.md) section.

## NICE Networking Library
[NICE Networking](https://github.com/mfitzer/NICE-Networking) is networking library I'm developing on top of this repository. It aims to replicate features from the old Unity networking system such as NetworkTransforms and SyncVars. Feel free to check it out and clone the repository. It includes documentation and samples to get you started.
