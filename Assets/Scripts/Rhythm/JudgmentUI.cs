using UnityEngine;
using UnityEngine.UI;

public class JudgmentUI : MonoBehaviour
{
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private Image judgmentLabel;
    [SerializeField] private Text comboText;
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite greatSprite;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite badSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private float labelVisibleSeconds = 0.4f;

    private int combo;
    private float labelHideAt;

    private void OnEnable() => judgmentSystem.OnJudged.AddListener(HandleJudged);
    private void OnDisable() => judgmentSystem.OnJudged.RemoveListener(HandleJudged);

    private void HandleJudged(int lane, Grade grade)
    {
        combo = (grade == Grade.Miss || grade == Grade.Bad) ? 0 : combo + 1;
        comboText.text = combo.ToString();

        judgmentLabel.sprite = SpriteFor(grade);
        judgmentLabel.enabled = true;
        labelHideAt = Time.time + labelVisibleSeconds;
    }

    private Sprite SpriteFor(Grade grade)
    {
        switch (grade)
        {
            case Grade.Perfect: return perfectSprite;
            case Grade.Great: return greatSprite;
            case Grade.Good: return goodSprite;
            case Grade.Bad: return badSprite;
            default: return missSprite;
        }
    }

    private void Update()
    {
        if (judgmentLabel.enabled && Time.time >= labelHideAt)
        {
            judgmentLabel.enabled = false;
        }
    }
}
