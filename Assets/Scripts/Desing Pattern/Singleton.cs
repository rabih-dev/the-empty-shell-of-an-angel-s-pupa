using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton : MonoBehaviour
{
    private static Singleton instance;

    // Content
    #region DATA
    public DialogueManager dialogueManager;
    #endregion DATA 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public static Singleton GetInstance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Singleton>();


                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("Singleton");
                    instance = singletonObject.AddComponent<Singleton>();
                }
            }
            return instance;
        }
    }


    

}
