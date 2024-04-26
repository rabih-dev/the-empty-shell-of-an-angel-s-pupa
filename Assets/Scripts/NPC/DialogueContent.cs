using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueContent
{
    [TextArea(1, 1)] public string name;
    [TextArea(1, 3)] public string[] sentences;
    public Sprite portrait;

    public AudioClip voice;

}
