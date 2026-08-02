using System;
using UnityEngine;


[Serializable]
public class Conversation
{
    [TextArea(2, 5)]
    public string[] lines;
}
