using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueText : ManagedBehaviour
{
    // 事件：文本抖动（参数为强度）
    public event System.Action<float> Shake;
    // 事件：触发动画（参数为动画名）
    public event System.Action<string> Animation;
    // 事件：一行文本显示完毕
    public event System.Action CompletedLine;
    // 事件：强制结束当前行
    public event System.Action ForceEndLine;
    // 事件：情感切换
    public event System.Action<Emotion> EmotionChange;
    // 事件：显示某个字符（参数为消息和索引）
    public event System.Action<string, int> DisplayCharacter;

    // 当前是否正在播放消息
    public bool PlayingMessage { get; private set; }
    // 是否到达可见字符末尾
    public bool EndOfVisibleCharacters { get; private set; }
    // 语音音调调整
    public float VoicePitchAdjust { get; private set; }
    // 当前UI是否激活
    private bool Active => uiText != null && uiText.gameObject.activeInHierarchy;

    [SerializeField]
    private Text uiText = default; // 用于显示文本的UI组件

    private List<Text> spawnedLetters = new List<Text>(); // 动态生成的字母对象列表

    private float characterFrequency; // 字符显示频率
    private Color currentColor = Color.black; // 当前文本颜色
    private bool skipToEnd; // 是否跳到文本末尾

    private const float DEFAULT_FREQUENCY = 7.5f; // 默认字符频率
    private const float DIALOGUE_SPEED = 1f; // 对话速度

    [SerializeField]
    private Color defaultColor = Color.black; // 默认文本颜色

    /// <summary>
    /// 初始化，设置默认颜色
    /// </summary>
    protected override void ManagedInitialize()
    {
        currentColor = defaultColor;
    }

    /// <summary>
    /// 播放一条消息（逐字显示）
    /// </summary>
    public void PlayMessage(string message)
    {
        ResetToDefaults();

        if (Active)
        {
            StartCoroutine(PlayMessageSequence(message));
        }
    }

    /// <summary>
    /// 清除当前文本和字母对象
    /// </summary>
    public void Clear()
    {
        StopAllCoroutines();
        PlayingMessage = false;
        HideAllLetters();
    }

    /// <summary>
    /// 跳到文本末尾，立即显示全部内容
    /// </summary>
    public void SkipToEnd()
    {
        skipToEnd = true;
    }

    /// <summary>
    /// 重置为默认状态
    /// </summary>
    private void ResetToDefaults()
    {
        VoicePitchAdjust = 0f;
        currentColor = defaultColor;
        characterFrequency = DEFAULT_FREQUENCY;
        skipToEnd = false;

        EmotionChange?.Invoke(Emotion.Neutral);
    }

    /// <summary>
    /// 根据未格式化文本生成字母对象
    /// </summary>
    private void InstantiateLetters(string unformattedMessage)
    {
        spawnedLetters.ForEach(x => Destroy(x.gameObject));
        spawnedLetters.Clear();

        uiText.text = unformattedMessage;
        Canvas.ForceUpdateCanvases();

        foreach (var pair in CustomUI.GetTextLetterPositions(uiText))
        {
            spawnedLetters.Add(SpawnLetter(pair.Value, pair.Key));
        }

        uiText.text = "";
    }

    /// <summary>
    /// 生成单个字母对象
    /// </summary>
    private Text SpawnLetter(char c, Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x * 100) / 100;
        pos.y = 0f;

        var obj = new GameObject(c.ToString());
        obj.transform.SetParent(uiText.transform.parent);
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one;
        obj.layer = uiText.gameObject.layer;

        var letter = obj.AddComponent<Text>();
        letter.text = c.ToString();
        letter.font = uiText.font;
        letter.fontSize = uiText.fontSize;
        letter.lineSpacing = uiText.lineSpacing;
        letter.alignment = TextAnchor.MiddleCenter;
        letter.raycastTarget = false;

        return letter;
    }

    /// <summary>
    /// 显示到指定索引的所有字母
    /// </summary>
    private void RevealLettersUpToIndex(int index)
    {
        for (int i = 0; i < spawnedLetters.Count; i++)
        {
            spawnedLetters[i].gameObject.SetActive(i <= index);
        }
    }

    /// <summary>
    /// 隐藏所有字母
    /// </summary>
    private void HideAllLetters()
    {
        RevealLettersUpToIndex(-1);
    }

    /// <summary>
    /// 协程：逐字显示消息，解析指令
    /// </summary>
    private IEnumerator PlayMessageSequence(string message)
    {
        PlayingMessage = true;
        EndOfVisibleCharacters = false;

        string unformattedMessage = DialogueParser.GetUnformattedMessage(message);
        string shownUnformatted = "";

        InstantiateLetters(unformattedMessage);

        int parsingIndex = 0;
        int letterIndex = 0;
        while (message.Length > parsingIndex)
        {
            string dialogueCode = DialogueParser.GetDialogueCode(message, parsingIndex);
            if (!string.IsNullOrEmpty(dialogueCode))
            {
                parsingIndex += dialogueCode.Length;
                yield return ConsumeCode(dialogueCode);
            }
            else
            {
                if (message[parsingIndex] != ' ')
                {
                    DisplayCharacter?.Invoke(message, parsingIndex);

                    RevealLettersUpToIndex(letterIndex);
                    letterIndex++;
                }

                shownUnformatted += message[parsingIndex];
                parsingIndex++;

                if (unformattedMessage == shownUnformatted)
                {
                    EndOfVisibleCharacters = true;
                }

                float adjustedFrequency = Mathf.Clamp(characterFrequency * 0.01f * DIALOGUE_SPEED, 0.01f, 0.2f);
                float waitTimer = 0f;
                while (!skipToEnd && waitTimer < adjustedFrequency)
                {
                    waitTimer += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        CompletedLine?.Invoke();
        PlayingMessage = false;
    }

    /// <summary>
    /// 解析并执行对话指令（如等待、变色、动画等）
    /// </summary>
    private IEnumerator ConsumeCode(string code)
    {
        if (code.StartsWith("[end"))
        {
            ForceEndLine?.Invoke();
        }
        else if (code.StartsWith("[e"))
        {
            string emotionValue = DialogueParser.GetStringValue(code, "e");
            var emotion = (Emotion)System.Enum.Parse(typeof(Emotion), emotionValue);
            EmotionChange?.Invoke(emotion);
        }
        else if (code.StartsWith("[w"))
        {
            float waitTimer = 0f;
            float waitLength = DialogueParser.GetFloatValue(code, "w");

            while (!skipToEnd && waitTimer < waitLength)
            {
                waitTimer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
        else if (code.StartsWith("[t"))
        {
            characterFrequency = DialogueParser.GetFloatValue(code, "t");
            if (characterFrequency == 0f)
            {
                characterFrequency = DEFAULT_FREQUENCY;
            }
        }
        else if (code.StartsWith("[c"))
        {
            currentColor = DialogueParser.GetColorFromCode(code, defaultColor);
        }
        else if (code.StartsWith("[shake:"))
        {
            float intensity = DialogueParser.GetFloatValue(code, "shake");
            Shake?.Invoke(intensity);
        }
        else if (code.StartsWith("[anim:"))
        {
            string trigger = DialogueParser.GetStringValue(code, "anim");
            Animation?.Invoke(trigger);
        }
        else if (code.StartsWith("[p"))
        {
            VoicePitchAdjust = DialogueParser.GetFloatValue(code, "p");
        }
    }
}
