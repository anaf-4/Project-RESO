using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private SongConductor conductor;
    [SerializeField] private JudgmentSystem judgmentSystem;

    private static readonly Key[] LaneKeys = { Key.D, Key.F, Key.J, Key.K };

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int lane = 0; lane < LaneKeys.Length; lane++)
        {
            if (keyboard[LaneKeys[lane]].wasPressedThisFrame)
            {
                judgmentSystem.TryHit(lane, conductor.SongTimeSeconds);
            }
        }
    }
}
