using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Beginner2D
{
    public class StartScreen : MonoBehaviour
    {
        void Awake()
        {
            RuntimeUIEventSystem.EnsurePresent();
        }

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            Button startButton = document.rootVisualElement.Q<Button>("StartButton");
            if (startButton != null)
                startButton.clicked += StartCampaign;
        }

        void OnDisable()
        {
            UIDocument document = GetComponent<UIDocument>();
            if (document == null)
                return;
            Button startButton = document.rootVisualElement.Q<Button>("StartButton");
            if (startButton != null)
                startButton.clicked -= StartCampaign;
        }

        void StartCampaign()
        {
            SceneManager.LoadScene("MyLevel");
        }
    }
}
