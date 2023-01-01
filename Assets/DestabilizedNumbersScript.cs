using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class DestabilizedNumbersScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    public Renderer backing;
    public Material masterBackMat;
    public TextMesh displayText;

    private KMAudio.KMAudioRef audioRef;
    private Coroutine sequence;
    private Coroutine glitch;
    private readonly string[] glitchChars = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "+", "-", "_", "=", "'", "\"" };
    private bool showSequence = false;
    private bool executeChange = false;
    private bool manuallyStopped = false;
    private int glitchIndex = -1;
    private int opType;
    private int opValue;
    private int lastMade = -1;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        Material temp = new Material(masterBackMat);
        backing.material = temp;
        StartCoroutine(AnimateBacking(temp));
        StartCoroutine(ChangeDisplayColor());
    }

    void Start()
    {
        opType = UnityEngine.Random.Range(0, 2);
        opValue = UnityEngine.Random.Range(0, 501);
        Debug.LogFormat("[Destabilized Numbers #{0}] Operation: {1}{2}", moduleId, new string[] { "+", "×" }[opType], opValue);
    }

    void Update()
    {
        if (showSequence && executeChange)
        {
            executeChange = false;
            if (lastMade == -1)
                lastMade = UnityEngine.Random.Range(0, 101);
            audioRef = audio.PlaySoundAtTransformWithRef("music", transform);
            sequence = StartCoroutine(Sequence());
        }
        else if (!showSequence && executeChange)
        {
            executeChange = false;
            audioRef.StopSound();
            StopCoroutine(sequence);
            sequence = null;
            if (glitch != null)
            {
                StopCoroutine(glitch);
                glitch = null;
            }
            manuallyStopped = true;
            displayText.text = "";
        }
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0)
            {
                showSequence = !showSequence;
                executeChange = true;
            }
            else if (index == 1)
            {
                if (lastMade == -1)
                {
                    Debug.LogFormat("[Destabilized Numbers #{0}] Strike, toggle has not been pressed at least once yet", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                    displayText.text = "";
                    return;
                }
                Debug.LogFormat("[Destabilized Numbers #{0}] Submit pressed, the number last on the display was {1}", moduleId, lastMade);
                if (displayText.text == "")
                {
                    Debug.LogFormat("[Destabilized Numbers #{0}] Strike, submitted nothing but expected {1}", moduleId, ApplyOperation(lastMade));
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else if (int.Parse(displayText.text) == ApplyOperation(lastMade))
                {
                    moduleSolved = true;
                    Debug.LogFormat("[Destabilized Numbers #{0}] Correctly submitted {1}, module solved", moduleId, ApplyOperation(lastMade));
                    GetComponent<KMBombModule>().HandlePass();
                    audio.PlaySoundAtTransform("solve", transform);
                    displayText.text = "GG_";
                }
                else
                {
                    Debug.LogFormat("[Destabilized Numbers #{0}] Strike, submitted {1} but expected {2}", moduleId, displayText.text, ApplyOperation(lastMade));
                    GetComponent<KMBombModule>().HandleStrike();
                    displayText.text = "";
                }
            }
            else if (displayText.text.Length < 3)
            {
                displayText.text += index - 2;
            }
        }
    }

    int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    int ApplyOperation(int num)
    {
        if (opType == 0)
            num += opValue;
        else if (opType == 1)
            num *= opValue;
        num = Mod(num, 1000);
        return num;
    }

    IEnumerator AnimateBacking(Material mat)
    {
        float offset = 0f;
        while (true)
        {
            yield return null;
            offset += Time.deltaTime * .5f;
            if (offset > 1)
                offset = 0;
            mat.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
    }

    IEnumerator GlitchDigit(int index)
    {
        while (true)
        {
            string num = lastMade.ToString("000");
            string choice = glitchChars.PickRandom();
            num = num.Remove(index, 1);
            num = num.Insert(index, choice);
            displayText.text = num;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator Sequence()
    {
        while (!moduleSolved)
        {
            if ((!manuallyStopped && UnityEngine.Random.Range(0, 2) == 0) || glitchIndex != -1)
            {
                if (glitchIndex == -1)
                    glitchIndex = UnityEngine.Random.Range(0, 3);
                glitch = StartCoroutine(GlitchDigit(glitchIndex));
            }
            else
                displayText.text = lastMade.ToString("000");
            yield return new WaitForSeconds(1.7f);
            manuallyStopped = false;
            if (glitch != null)
            {
                StopCoroutine(glitch);
                glitch = null;
                glitchIndex = -1;
            }
            lastMade = ApplyOperation(lastMade);
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                lastMade += UnityEngine.Random.Range(-100, 101);
                lastMade = Mod(lastMade, 1000);
            }
        }
    }

    IEnumerator ChangeDisplayColor()
    {
        while (true)
        {
            float choice = UnityEngine.Random.Range(0.5f, 1f);
            displayText.color = new Color(choice, choice, choice);
            yield return new WaitForSeconds(0.05f);
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} toggle [Presses the button labelled ""TOGGLE""] | !{0} submit <number> [Submits the specified number]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[0].OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a number to submit!";
            else
            {
                int temp = -1;
                if (!int.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror!f The specified number '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp < 0)
                {
                    yield return "sendtochaterror The specified number '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (showSequence)
                {
                    yield return "sendtochaterror You cannot submit a number right now!";
                    yield break;
                }
                yield return null;
                for (int i = 1; i < parameters.Length; i++)
                {
                    buttons[int.Parse(parameters[1][i].ToString()) + 2].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                buttons[1].OnInteract();
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (lastMade == -1)
        {
            buttons[0].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        if (showSequence)
        {
            buttons[0].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        string curr = displayText.text;
        string ans = ApplyOperation(lastMade).ToString();
        bool clrPress = false;
        if (curr.Length > ans.Length)
        {
            buttons[0].OnInteract();
            yield return new WaitForSeconds(0.1f);
            buttons[0].OnInteract();
            yield return new WaitForSeconds(0.1f);
            clrPress = true;
        }
        else
        {
            for (int i = 0; i < curr.Length; i++)
            {
                if (i == ans.Length)
                    break;
                if (curr[i] != ans[i])
                {
                    buttons[0].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    buttons[0].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    clrPress = true;
                    break;
                }
            }
        }
        int start = 0;
        if (!clrPress)
            start = curr.Length;
        for (int j = start; j < ans.Length; j++)
        {
            buttons[int.Parse(ans[j].ToString()) + 2].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        buttons[1].OnInteract();
    }
}