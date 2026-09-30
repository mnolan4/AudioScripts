using UnityEngine;
using UnityEngine.SceneManagement;

namespace AudioFeatureLab
{
    [AddComponentMenu("Audio Feature Lab/Return To Menu")]
    public class ReturnToMenu : MonoBehaviour
    {
        public KeyCode key = KeyCode.M;
        public string menuScene = "00_MainMenu";

        void Update()
        {
            if (Input.GetKeyDown(key))
                SceneManager.LoadScene(menuScene);
        }
    }
}
