using UnityEngine;

namespace AudioFeatureLab
{
    public static class BlendKeyboard
    {
        public static bool TryReadBlend(out float blend)
        {
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    blend = i / 8f;
                    return true;
                }
            }

            blend = 0f;
            return false;
        }
    }
}
