using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueAdm : MonoBehaviour
{
    //UI Components
    public Image portait;
    public Text dialogueTxt;
    public GameObject dialogueBox;

    public Queue<string> sentences;
    private PlayerController player;
    

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        sentences = new Queue<string>();
        dialogueBox.SetActive(false);
    }


    public void StartDialogue(DialogueContent content)
    {
        if (!dialogueBox.activeSelf)
        {
            dialogueBox.SetActive(true);
            player.isTalking = true;
            portait.sprite = content.portrait;

            sentences.Clear();

            foreach (string sentence in content.sentences)
            {
                sentences.Enqueue(sentence);
            }

            NextSentence();

        }
    }

    public void NextSentence()
    {
        if (sentences.Count <= 0)
        {
            EndDialogue();
            return;
        }
        string curSentence = sentences.Dequeue();
        StopAllCoroutines();
        StopCoroutine(LetterByLetter(curSentence));
    }

    public void EndDialogue()
    {
        dialogueBox.SetActive(true);
        player.isTalking = false;
    }

    IEnumerator LetterByLetter(string sentenceToSpell)
    {
        dialogueTxt.text = "";

        foreach (char letter in sentenceToSpell.ToCharArray()) 
        {
            dialogueTxt.text += letter;
        }
        yield return new WaitForSeconds(0.1f);
    }
}
