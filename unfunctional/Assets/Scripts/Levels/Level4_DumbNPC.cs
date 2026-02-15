using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 4: Dumb NPC with an unnecessarily long conversation.
/// A dialogue system with branching options that all lead to more talking.
/// The NPC says nothing useful but the player must exhaust all dialogue to proceed.
/// 
/// Attach to a root GameObject in the LEVEL4 scene.
/// </summary>
public class Level4_DumbNPC : LevelManager
{
    [Header("Level 4 - Dumb NPC Dialogue")]
    public Canvas dialogueCanvas;
    public Text npcNameText;
    public Text dialogueText;
    public Button[] optionButtons;          // Usually 2-3 dialogue option buttons
    public Text[] optionTexts;              // Text components on the option buttons

    [Header("NPC")]
    public GameObject npcObject;
    public float interactRange = 3f;
    public string npcName = "Gerald the Helpful";

    [Header("Typing Effect")]
    public float typingSpeed = 0.04f;       // Seconds per character
    public bool enableTypingEffect = true;

    [System.Serializable]
    public class DialogueNode
    {
        public string npcText;
        public string[] options;            // Player response options
        public int[] nextNodeIndices;       // Where each option leads (-1 = end)
    }

    [Header("Dialogue Tree")]
    public List<DialogueNode> dialogueTree = new List<DialogueNode>();

    private int currentNodeIndex = 0;
    private bool inDialogue = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private int nodesVisited = 0;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "NPC Conversation";
        levelDescription = "Talk to the NPC. All of it.";

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        // Build default dialogue tree if none assigned
        if (dialogueTree.Count == 0)
        {
            BuildDefaultDialogue();
        }

        HideOptions();
    }

    private void Update()
    {
        if (levelComplete) return;

        // Check for NPC interaction
        if (!inDialogue && InputManager.Instance != null && InputManager.Instance.InteractPressed)
        {
            TryStartDialogue();
        }
    }

    private void TryStartDialogue()
    {
        Camera cam = Camera.main;
        if (cam == null || npcObject == null) return;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);
        if (dist <= interactRange)
        {
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        inDialogue = true;
        currentNodeIndex = 0;
        nodesVisited = 0;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);

        if (InputManager.Instance != null)
            InputManager.Instance.UnlockCursor();

        ShowCurrentNode();
    }

    private void ShowCurrentNode()
    {
        if (currentNodeIndex < 0 || currentNodeIndex >= dialogueTree.Count)
        {
            EndDialogue();
            return;
        }

        DialogueNode node = dialogueTree[currentNodeIndex];
        nodesVisited++;

        if (npcNameText != null)
            npcNameText.text = npcName;

        // Show NPC text with typing effect
        if (enableTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(node.npcText, node));
        }
        else
        {
            if (dialogueText != null)
                dialogueText.text = node.npcText;
            ShowOptions(node);
        }
    }

    private IEnumerator TypeText(string text, DialogueNode node)
    {
        isTyping = true;
        HideOptions();

        if (dialogueText != null)
        {
            dialogueText.text = "";
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
        ShowOptions(node);
    }

    private void ShowOptions(DialogueNode node)
    {
        if (node.options == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < node.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                if (i < optionTexts.Length && optionTexts[i] != null)
                    optionTexts[i].text = node.options[i];

                int capturedIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(capturedIndex, node));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideOptions()
    {
        if (optionButtons == null) return;
        foreach (Button btn in optionButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
    }

    private void OnOptionSelected(int optionIndex, DialogueNode node)
    {
        if (node.nextNodeIndices != null && optionIndex < node.nextNodeIndices.Length)
        {
            currentNodeIndex = node.nextNodeIndices[optionIndex];
        }
        else
        {
            currentNodeIndex = -1;
        }

        ShowCurrentNode();
    }

    private void EndDialogue()
    {
        inDialogue = false;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        if (InputManager.Instance != null)
            InputManager.Instance.LockCursor();

        Debug.Log($"[Level4] Dialogue ended. Visited {nodesVisited} nodes.");
        CompleteLevel();
    }

    /// <summary>
    /// Builds a default annoying dialogue tree if none is assigned in the inspector.
    /// </summary>
    private void BuildDefaultDialogue()
    {
        dialogueTree = new List<DialogueNode>
        {
            // Node 0 - Greeting
            new DialogueNode
            {
                npcText = "Oh! A visitor! I haven't had a visitor in... well, I've never had a visitor actually. This is quite exciting. Let me tell you about my day. So I woke up this morning and—",
                options = new string[] { "I just need to get through the door.", "Tell me about your day." },
                nextNodeIndices = new int[] { 1, 2 }
            },
            // Node 1 - Door question redirect
            new DialogueNode
            {
                npcText = "Door? What door? Oh THAT door. Yes, yes. I know about the door. But first, have I told you about my collection of vintage spoons? I have over 300.",
                options = new string[] { "No, please just the door.", "Tell me about the spoons." },
                nextNodeIndices = new int[] { 3, 4 }
            },
            // Node 2 - Day story
            new DialogueNode
            {
                npcText = "So I woke up and my pillow was slightly to the left of where I usually put it. Can you believe that? Anyway, then I spent about 45 minutes deciding what to have for breakfast. I went with toast. Actually no, I had cereal. Wait, was it toast? Let me think...",
                options = new string[] { "It doesn't matter.", "Was it toast or cereal?" },
                nextNodeIndices = new int[] { 1, 5 }
            },
            // Node 3 - Insist about door
            new DialogueNode
            {
                npcText = "You're very focused. I admire that. Reminds me of my uncle. He was focused too. Focused on collecting bottle caps. He had 12,000 of them. Would you like to hear about each one?",
                options = new string[] { "PLEASE just tell me about the door.", "Actually, yes." },
                nextNodeIndices = new int[] { 6, 7 }
            },
            // Node 4 - Spoon story
            new DialogueNode
            {
                npcText = "My favorite spoon is number 47. It has a slight bend in the handle from when I used it to dig a very small hole in my garden. I was planting a seed. The seed never grew. I think about that seed sometimes.",
                options = new string[] { "Can we talk about the door now?", "What kind of seed?" },
                nextNodeIndices = new int[] { 6, 8 }
            },
            // Node 5 - Breakfast debate
            new DialogueNode
            {
                npcText = "You know what, I think it was actually a toast-cereal hybrid. I put the cereal on the toast. Revolutionary, right? I should patent that. Do you know how patents work? Because I don't.",
                options = new string[] { "About that door...", "That sounds terrible." },
                nextNodeIndices = new int[] { 6, 9 }
            },
            // Node 6 - Finally about the door
            new DialogueNode
            {
                npcText = "Fine, fine. The door. You want to know about the door. Here's the thing about the door: it's a door. It has hinges. And a handle. You push it or pull it. Actually, I forget which one. Maybe it slides? No wait, I think you have to say a password.",
                options = new string[] { "What's the password?", "I'll just try the handle." },
                nextNodeIndices = new int[] { 10, 11 }
            },
            // Node 7 - Bottle caps
            new DialogueNode
            {
                npcText = "Bottle cap number 1 was a Coca-Cola cap from 1987. It was red. Bottle cap number 2 was also a Coca-Cola cap from 1987. Also red. Bottle cap number 3— you know what, this might take a while. Let's skip to cap 11,999.",
                options = new string[] { "Please, the door.", "What about cap 12,000?" },
                nextNodeIndices = new int[] { 6, 6 }
            },
            // Node 8 - Seed story
            new DialogueNode
            {
                npcText = "It was a mystery seed. Found it in my pocket. Could have been anything. A tree, a flower, a small civilization. We'll never know. Anyway, that's unrelated to anything. What were we talking about?",
                options = new string[] { "The door.", "Your spoons." },
                nextNodeIndices = new int[] { 6, 4 }
            },
            // Node 9 - Toast cereal reaction
            new DialogueNode
            {
                npcText = "Terrible?! TERRIBLE?! It was the greatest culinary invention since... since regular toast! And regular cereal! Combined! I'll have you know three people have tried it and only two of them were hospitalized.",
                options = new string[] { "I need to go through that door.", "Are they okay?" },
                nextNodeIndices = new int[] { 6, 6 }
            },
            // Node 10 - Password
            new DialogueNode
            {
                npcText = "The password is... hmm. I wrote it down somewhere. On my hand I think. Let me check. No, that's my grocery list. Eggs, milk, more spoons... Oh! I remember now! The password is 'please'. Or 'open sesame'. Or 'Gerald is the best'. One of those three.",
                options = new string[] { "Please.", "Open sesame.", "Gerald is the best." },
                nextNodeIndices = new int[] { 12, 12, 12 }
            },
            // Node 11 - Try handle
            new DialogueNode
            {
                npcText = "Good luck with that. The handle has been broken since before I got here. I've been meaning to fix it but I've been busy cataloguing my spoon collection. And my bottle cap collection. And my collection of collections.",
                options = new string[] { "So what DO I do?", "A collection of collections?" },
                nextNodeIndices = new int[] { 10, 10 }
            },
            // Node 12 - Final
            new DialogueNode
            {
                npcText = "It worked! Or maybe the door was never actually locked. I honestly don't remember. Anyway, it was lovely chatting with you. If you ever want to hear about my spoons in more detail, you know where to find me. Actually, you don't. I move around a lot. Goodbye!",
                options = new string[] { "Goodbye, Gerald." },
                nextNodeIndices = new int[] { -1 }
            }
        };
    }
}
