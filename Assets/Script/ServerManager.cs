using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.IO;


public class ServerManager : MonoBehaviour
{

    // Use this for initialization
    List<User> ConnectedUsers;
    ServerConfiguration config;
    Map selectedMap;
    float timeLeft;

    float time2newmatch = 0;

    bool matchEnded;

    void Start()
    {
        ConnectedUsers = new List<User>();
        config = getConfig(@"config.json");
        NetworkServer.Listen(config.port);
        timeLeft = config.timeLimit;
        selectedMap = MapManager.getMapByName(config.mapName);
        NetworkServer.RegisterHandler(MsgType.Connect, OnConnect);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnDisconnect);
        NetworkServer.RegisterHandler(777, OnAuth);
        NetworkServer.RegisterHandler(790, OnReady);
        NetworkServer.RegisterHandler(899, OnPlayerUpdate);
        NetworkServer.RegisterHandler(900, OnPlayerRotationChange);
        NetworkServer.RegisterHandler(901, OnPlayerPosCorrection);
        NetworkServer.RegisterHandler(902, OnPlayerDirChange);
        NetworkServer.RegisterHandler(903, OnPlayerShoot);
        NetworkServer.RegisterHandler(904, OnDead);
        NetworkServer.RegisterHandler(905, OnPlayerDamaged);
        NetworkServer.RegisterHandler(907, OnPlayerArmRot);
        NetworkServer.RegisterHandler(908, OnPlayerJump);
    }

    void OnConnect(NetworkMessage netMsg)
    {
        Debug.Log("User Connected! " + netMsg.conn.connectionId);
        Configuration c = new Configuration();
        c.kills = config.kills;
        c.mapName = config.mapName;
        c.respawnTime = config.respawnTime;
        c.time2newmatch = time2newmatch;
        c.timeLeft = timeLeft;
        c.timeLimit = config.timeLimit;
        NetworkServer.SendToClient(netMsg.conn.connectionId, 555, c);
    }

    void OnDisconnect(NetworkMessage netMsg)
    {
        ConnectedUsers.RemoveAll(x => x.id == netMsg.conn.connectionId);
        NetInt userDisconnect = new NetInt();
        userDisconnect.value = netMsg.conn.connectionId;
        sendToAllExcept(netMsg.conn.connectionId, 810, userDisconnect);
        /*if(ConnectedUsers.Count==0){
			 timeLeft=config.timeLimit;
		}*/
        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,810,userDisconnect);
		}*/
    }

    void OnReady(NetworkMessage netMsg)
    {
        NetArray netArray = netMsg.ReadMessage<NetArray>();
        netArray.value = netArray.value.Except(ConnectedUsers.Select(x => x.id)).ToArray();
        NetworkServer.SendToClient(netMsg.conn.connectionId, 791, netArray);
    }

    void OnAuth(NetworkMessage netMsg)
    {
        ServerAuth serverAuth = netMsg.ReadMessage<ServerAuth>();
        Debug.Log("User Authenticated! " + serverAuth.username + ":" + netMsg.conn.connectionId);


        //TODO:randomize
        Vector3 position = selectedMap.getRandomSpawnPoint();
        //TODO:randomize
        Welcome welcome = new Welcome();
        welcome.id = netMsg.conn.connectionId;
        welcome.users = ConnectedUsers.ToArray();
        welcome.position = position;
        NetworkServer.SendToClient(netMsg.conn.connectionId, 778, welcome);
        User u = new User();
        u.id = netMsg.conn.connectionId;
        u.username = serverAuth.username;
        u.media = serverAuth.media;
        u.position = position;
        u.rotation = Quaternion.identity;
        u.direction = Vector3.zero;
        u.kills = 0;
        ConnectedUsers.Add(u);

        sendToAllExcept(netMsg.conn.connectionId, 811, u);
        /*foreach(User current in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(current.id,811,u);
		}*/

    }

    void OnPlayerRotationChange(NetworkMessage netMsg)
    {
        NetQuaternion rotUpdate = netMsg.ReadMessage<NetQuaternion>();
        PlayerQuaternion playerRot = new PlayerQuaternion();
        playerRot.id = netMsg.conn.connectionId;
        playerRot.quaternion = rotUpdate.value;
        sendToAllExcept(netMsg.conn.connectionId, 910, playerRot);

        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,910,playerRot);
		}*/
        ConnectedUsers.Find(x => x.id == netMsg.conn.connectionId).rotation = rotUpdate.value;
    }

    void OnPlayerJump(NetworkMessage netMsg)
    {
        NetInt id = new NetInt();
        id.value = netMsg.conn.connectionId;
        sendToAllExcept(netMsg.conn.connectionId, 918, id);
    }



    void OnPlayerArmRot(NetworkMessage netMsg)
    {
        NetQuaternion netArmRot = netMsg.ReadMessage<NetQuaternion>();
        PlayerQuaternion armRot = new PlayerQuaternion();
        armRot.id = netMsg.conn.connectionId;
        armRot.quaternion = netArmRot.value;
        sendToAllExcept(netMsg.conn.connectionId, 917, armRot);

        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,910,playerRot);
		}*/
    }

    void OnPlayerUpdate(NetworkMessage netMsg)
    {
        NetUpdate netUpdate = netMsg.ReadMessage<NetUpdate>();
        PlayerUpdate playerUpdate = new PlayerUpdate();
        playerUpdate.id = netMsg.conn.connectionId;
        playerUpdate.dead = netUpdate.dead;
        playerUpdate.kills = netUpdate.kills;
        playerUpdate.dir = netUpdate.dir;
        playerUpdate.pos = netUpdate.pos;
        playerUpdate.rot = netUpdate.rot;
        sendToAllExcept(netMsg.conn.connectionId, 898, playerUpdate);
    }


    void OnPlayerDamaged(NetworkMessage netMsg)
    {
        PlayerShoot im = netMsg.ReadMessage<PlayerShoot>();
        PlayerShoot om = new PlayerShoot();
        om.id = netMsg.conn.connectionId;
        om.damage = im.damage;
        Debug.Log("Player damaged! " + im.damage + " " + om.damage);
        NetworkServer.SendToClient(im.id, 915, om);
    }

    void OnPlayerPosCorrection(NetworkMessage netMsg)
    {
        NetVect3 posUpdate = netMsg.ReadMessage<NetVect3>();
        PlayerVect3 playerPos = new PlayerVect3();
        playerPos.id = netMsg.conn.connectionId;
        playerPos.vect3 = posUpdate.value;
        sendToAllExcept(netMsg.conn.connectionId, 911, playerPos);
        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,911,playerPos);
		}*/
        ConnectedUsers.Find(x => x.id == netMsg.conn.connectionId).position = posUpdate.value;
    }

    void OnPlayerDirChange(NetworkMessage netMsg)
    {
        NetDir dirUpdate = netMsg.ReadMessage<NetDir>();
        PlayerDir playerDir = new PlayerDir();
        playerDir.id = netMsg.conn.connectionId;
        playerDir.vect3 = dirUpdate.value;
        playerDir.b = dirUpdate.b;
        sendToAllExcept(netMsg.conn.connectionId, 912, playerDir);

        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,912,playerDir);
		}*/
        ConnectedUsers.Find(x => x.id == netMsg.conn.connectionId).direction = dirUpdate.value;
    }

    void OnPlayerShoot(NetworkMessage netMsg)
    {
        NetInt shootUpdate = netMsg.ReadMessage<NetInt>();
        PlayerShoot playerShoot = new PlayerShoot();
        playerShoot.id = netMsg.conn.connectionId;
        playerShoot.damage = shootUpdate.value;
        sendToAllExcept(netMsg.conn.connectionId, 913, playerShoot);
        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,913,playerShoot);
		}*/
    }

    void OnDead(NetworkMessage netMsg)
    {
        //take care of killer id
        NetInt dead = netMsg.ReadMessage<NetInt>();
        PlayerDead playerDead = new PlayerDead();
        playerDead.victim = netMsg.conn.connectionId;
        playerDead.killer = dead.value;
        sendToAllExcept(netMsg.conn.connectionId, 914, playerDead);
        ConnectedUsers.Find(x => x.id == netMsg.conn.connectionId).dead = true;
        /*foreach(User u in ConnectedUsers.FindAll(x => x.id != netMsg.conn.connectionId)){
			NetworkServer.SendToClient(u.id,914,playerDead);
		}*/
        StartCoroutine(respawn(netMsg.conn.connectionId));

        User u = ConnectedUsers.Find(x => x.id == dead.value);
        u.kills++;
        if (u.kills >= config.kills)
        {
            timeLeft = 0;
        }
    }


    ServerConfiguration getConfig(string path)
    {
        StreamReader reader = new StreamReader(path);
        string c = reader.ReadToEnd();
        reader.Close();
        return JsonUtility.FromJson<ServerConfiguration>(c);
    }


    // Update is called once per frame
    void Update()
    {
        if (ConnectedUsers.Count > 0)
        {
            if (timeLeft > 0 && !matchEnded)
            {
                timeLeft -= Time.deltaTime;
            }
            else if (!matchEnded)
            {
                matchEnded = true;
                time2newmatch = config.respawnTime;
                NetworkServer.SendToAll(100, new EmptyMessage());
            }
            else if (matchEnded && time2newmatch > 0)
            {
                time2newmatch -= Time.deltaTime;
            }
            else
            {
                matchEnded = false;
                timeLeft = config.timeLimit;
                foreach (User u in ConnectedUsers)
                {
                    u.kills = 0;
                }
                respawnAll();
                NetVect3PArray netVect3PArray = new NetVect3PArray();
                netVect3PArray.array = new PlayerVect3[ConnectedUsers.Count];
                for (int i = 0; i < ConnectedUsers.Count; i++)
                {
                    netVect3PArray.array[i] = new PlayerVect3();
                    netVect3PArray.array[i].id = ConnectedUsers[i].id;
                    netVect3PArray.array[i].vect3 = ConnectedUsers[i].position;
                }

                NetworkServer.SendToAll(101, netVect3PArray);
            }
        }
    }


    public IEnumerator respawn(int id)
    {
        yield return new WaitForSeconds(config.respawnTime);
        Vector3 respawn = selectedMap.getRandomSpawnPoint();
        NetVect3 vect3 = new NetVect3();
        vect3.value = respawn;
        PlayerVect3 playerVect = new PlayerVect3();
        playerVect.id = id;
        playerVect.vect3 = respawn;
        NetworkServer.SendToClient(id, 926, vect3);
        sendToAllExcept(id, 916, playerVect);
        ConnectedUsers.Find(x => x.id == id).dead = false;
    }

    public void respawnAll()
    {
        foreach (User u in ConnectedUsers)
        {
            Vector3 respawn = selectedMap.getRandomSpawnPoint();
            u.position = respawn;
        }
    }

    public void sendToAllExcept(int playerId, short msgId, MessageBase o)
    {
        foreach (User u in ConnectedUsers.FindAll(x => x.id != playerId))
        {
            NetworkServer.SendToClient(u.id, msgId, o);
        }
    }


    [System.Serializable]
    public class Configuration : MessageBase
    {
        public int timeLimit;
        public float timeLeft;
        public float time2newmatch;
        public int respawnTime;
        public int kills;
        public string mapName;

    }


    [System.Serializable]
    public class ServerConfiguration : MessageBase
    {
        public int port;
        public int timeLimit;
        public int respawnTime;
        public int kills;
        public string mapName;

    }

    [System.Serializable]
    public class ServerAuth : MessageBase
    {
        public string username;
        public int media;
    }

    [System.Serializable]
    public class LoginRequest
    {
        public LoginData data;
    }
    [System.Serializable]
    public class LoginData
    {
        public AuthData auth;
    }

    [System.Serializable]
    public class AuthData
    {
        public bool loggedIn;
        public UserData accountInfo;
    }

    [System.Serializable]
    public class UserData
    {
        public int id;
        public string nome;
        public string cognome;
        public string type="";
        public Dictionary<string,List<int>> materie;
    }

	[System.Serializable]
    public class Welcome : MessageBase
    {
        public int id;
        public User[] users;
		public Vector3 position;
    }

    [System.Serializable]    
	public class User : MessageBase{
		public string username;
		public int id;
        public int kills;
        public bool dead;

        public int media;

        public Vector3 position;

        public Quaternion rotation;

        public Vector3 direction;
	}

    [System.Serializable]
    public class NetArray : MessageBase
    {
		public int[] value;
    }

    [System.Serializable]
    public class NetUpdate : MessageBase
    {
        public int kills;
        public bool dead;
		public Quaternion rot;
        public Quaternion armRot;
		public Vector3 pos;
		public NetDir dir;
    }


	[System.Serializable]
    public class PlayerUpdate : MessageBase
    {
		public int id;

        public int kills;
        public bool dead;
		public Vector3 pos;
		public NetDir dir;
		public Quaternion rot;

        public Quaternion armRot;

    }
	

    [System.Serializable]
    public class NetQuaternion : MessageBase
    {
		public Quaternion value;
    }

    [System.Serializable]
    public class NetVect3 : MessageBase
    {
		public Vector3 value;
    }

    [System.Serializable]
    public class NetDir : MessageBase
    {
		public Vector3 value;
        public bool b;
    }


	[System.Serializable]
    public class NetInt : MessageBase
    {
        public int value;
    }

	[System.Serializable]
    public class PlayerDead : MessageBase
    {
		public int killer;
        public int victim;
    }

    [System.Serializable]
    public class PlayerQuaternion : MessageBase
    {
        public int id;
		public Quaternion quaternion;
    }

    [System.Serializable]
    public class PlayerVect3 : MessageBase
    {
        public int id;
		public Vector3 vect3;
    }

    [System.Serializable]
    public class PlayerDir : MessageBase
    {
        public int id;
		public Vector3 vect3;
        public bool b;
    }

    [System.Serializable]
	public class NetVect3PArray : MessageBase{
		public PlayerVect3[] array;
	}

	[System.Serializable]
    public class PlayerShoot : MessageBase
    {
        public int id;
		public int damage;
    }
}