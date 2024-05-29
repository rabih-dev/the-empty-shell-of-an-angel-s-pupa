using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DialogueManager : MonoBehaviour
{
    //UI Components
    public Image portait;
    public Text dialogueTxt;
    public GameObject dialogueBox;
    public Text name;
    private AudioClip npcVoice;
    public bool finishedSentence;
    private string curSentence;

    public Queue<string> sentences;
    private PlayerController player;
    public AudioSource audioS;


    void Start()
    {
        audioS = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        sentences = new Queue<string>();
        dialogueBox.SetActive(false);
        finishedSentence = true;
    }


    public void StartDialogue(DialogueContent content)
    {
        if (!dialogueBox.activeSelf)
        {
            dialogueBox.SetActive(true);
            portait.sprite = content.portrait;
            name.text = content.name;
            

            audioS.clip = content.voice;
            sentences.Clear();

            foreach (string sentence in content.sentences)
            {
                sentences.Enqueue(sentence);
            }
            print("troquei tudo e vou chamar a proxima frase");
            NextSentence();
        }
    }

    public void NextSentence()
    {
        print("coisas serao ditas");

        if (sentences.Count == 0)
        {
            finishedSentence = true;
            EndDialogue();
            return;
        }

        curSentence = sentences.Dequeue();
        StopAllCoroutines();
        finishedSentence = false;
        StartCoroutine(LetterByLetter(curSentence));
    }

    public void EndDialogue()
    {
        dialogueBox.SetActive(false);
        player.isTalking = false;
    }

    IEnumerator LetterByLetter(string sentenceToSpell)
    {
        dialogueTxt.text = "";

        foreach (char letter in sentenceToSpell.ToCharArray())
        {
            dialogueTxt.text += letter;
            audioS.Play();

            yield return new WaitForSeconds(0.1f);

            if (dialogueTxt.text == sentenceToSpell)
            {
                finishedSentence = true;
            }
        }
    }

    public void SkipLetterByLetter()
    {
        finishedSentence = true;
        StopAllCoroutines();
        dialogueTxt.text = curSentence;
    }

}
