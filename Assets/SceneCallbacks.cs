using System.Collections;
using System.Collections.Generic;
using Bolt;
using UdpKit;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomProtocolToken : Bolt.IProtocolToken
{
    public string ArbitraryData;
    public string password;

    public void Read(UdpPacket packet)
    {
        ArbitraryData = packet.ReadString();
        password = packet.ReadString();
    }

    public void Write(UdpPacket packet)
    {
        packet.WriteString(ArbitraryData);
        packet.WriteString(password);
    }
}

public class SceneCallbacks : Bolt.GlobalEventListener
{
    public SceneLoader sceneLoader;

    public override void SceneLoadLocalDone(string scene)
    {
        if (BoltNetwork.IsServer)
        {
            sceneLoader.LoadWorld();
        }
    }

    public override void Connected(BoltConnection connection)
    {
        if (BoltNetwork.IsClient)
        {
            Debug.Log(BoltNetwork.Server.AcceptToken);
            BoltConsole.Write(BoltNetwork.Server.AcceptToken.ToString());
        }
    }


    public override void ConnectRequest(UdpEndPoint endpoint, IProtocolToken token)
    {
        if (BoltNetwork.IsServer)
        {
            RoomProtocolToken myToken = new RoomProtocolToken()
            {
                ArbitraryData = staticData.myAdditiveWorld,
                password = "password"
            };

            BoltNetwork.Accept(endpoint, myToken);

        }
    }

    public override void SceneLoadRemoteDone(BoltConnection connection)
    {
        if (BoltNetwork.IsServer)
        {
            var evnt = LogEvent.Create(connection);
            evnt.Message = staticData.myAdditiveWorld;
            evnt.Send();
        }
    }


    public override void OnEvent(LogEvent evnt)
    {
        Debug.Log(evnt.Message);
        BoltConsole.Write(evnt.Message);
        staticData.myAdditiveWorld = evnt.Message;
        sceneLoader.LoadWorld();
    }

    public override void BoltShutdownBegin(AddCallback registerDoneCallback)
    {
        registerDoneCallback(Test0);
    }

    void Test0()
    {
        SceneManager.LoadScene("menu");
    }
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            BoltNetwork.Shutdown();

        }
    }
}
