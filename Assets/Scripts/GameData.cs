using UnityEngine;
using System;

[Serializable]
public class InputMessage
{
    public string type = "input";
    public Movement movement;
}

[Serializable]
public class Movement
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class InitMessage
{
    public string type;
    public int playerId;
    public PlayerData[] players;
    public DummyData[] dummies;
}
[Serializable]
public class UpdateMessage
{
    public string type;
    public PlayerData[] players;
    public DummyData[] dummies;
}

[Serializable]
public class PlayerData
{
    public int id;
    public float[] pos;
    public float[] color;
}

[Serializable]
public class DummyData
{
    public int id;
    public float[] pos;
}