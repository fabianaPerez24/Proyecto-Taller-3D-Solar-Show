using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//[System.Serializable]
//public struct Dialogue
//{
//    public Sprite sprite;
//    public string CharName;
//    [TextArea(4, 6)] public string line;
//}


public class Dialogo : MonoBehaviour
{
    private bool didDialogueStart;
    private int lineaIndex;

    [SerializeField] float typingTime = 0.02f;
    [Header("Cuadro de Texto")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text NormalDialogueText;
    [SerializeField] private TMP_Text NameText;
    [SerializeField] private Image Imagen;

    [Header("Dialogos")]
    public RuntimeDialogueGraph RuntimeGraph;

    private Dictionary<string, RuntimeDialogueNode> _nodeLookup = new Dictionary<string, RuntimeDialogueNode>();
    private RuntimeDialogueNode _currentNode;
    //[SerializeField] private Dialogue[] dialogueLine;

    [Header("Eventos")]
    [SerializeField] UnityEvent OntriggerEnter;
    [SerializeField] UnityEvent DialogueEndEvent;

    [Header("Variables")]
    [SerializeField] float TimeScale;
    [SerializeField] bool CanChangeTime;
    [SerializeField] bool canDisableCanva;

    [Header("Sonidos")]
    public AudioClip niftyVoice;
    public AudioClip Qwarkvoice;
    public AudioSource Robotvoice;


    void Start()
    {
        foreach (var node in RuntimeGraph.AllNodes)
        {
            _nodeLookup[node.NodeID] = node;
        }
    }
    void Update()
    {
        if (didDialogueStart)
        {

            if (NormalDialogueText.text == _currentNode.DialogueText)
            {
                NextDialogueLine();
            }
            //else
            //{ 
            //    StopAllCoroutines(); 
            //    dialogueText.text = dialogueLine[lineaIndex].line;
            //}
        }

        if (lineaIndex == _nodeLookup.Count) DialogueEndEvent.Invoke();
    }

    public void StartDialogue()
    {
        didDialogueStart = true;
        dialoguePanel.SetActive(true);
        lineaIndex = 0;

        if (CanChangeTime) Time.timeScale = TimeScale; //afecta en el movimiento del player

        StartCoroutine(ShowLine(RuntimeGraph.EntryNodeID));

    }

    private void NextDialogueLine()
    {
        lineaIndex++;
        if (lineaIndex < _nodeLookup.Count)
        {
            StartCoroutine(ShowLine(_currentNode.NextNodeID));
        }

        else
        {
            StartCoroutine(LastDialogue());
        }
    }

    private IEnumerator ShowLine(string NodeID)
    {
        if (lineaIndex != 0) yield return new WaitForSecondsRealtime(1f);
        _currentNode = _nodeLookup[NodeID];

        Imagen.sprite = _currentNode.SpriteCharacter;
        NameText.text = _currentNode.SpeakerName;

        if(_currentNode.SpeakerName == "Nifty")
        {
            Robotvoice.clip = niftyVoice;

            Robotvoice.Play();
        }

        if(_currentNode.SpeakerName == "Qwark")
        {
            Robotvoice.clip = Qwarkvoice;

            Robotvoice.Play();
        }

        NormalDialogueText.text = string.Empty;

        foreach (char ch in _currentNode.DialogueText)
        {
            NormalDialogueText.text += ch;
            yield return new WaitForSecondsRealtime(typingTime);
        }
    }

    private IEnumerator LastDialogue()
    {
        didDialogueStart = false;
        yield return new WaitForSecondsRealtime(2.5f);

        if(canDisableCanva) dialoguePanel.SetActive(false);

        Time.timeScale = 1f;

        Robotvoice.Stop();
        Destroy(this);

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            if (!didDialogueStart)
            {
                StartDialogue();

                if(OntriggerEnter != null)
                OntriggerEnter.Invoke();
            }
        }
    }

}
