using Assets.Scripts.GamePlay;
using TMPro;
using UnityEngine;

namespace ErrorDisplay
{
    public class ErrorHandler : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _errorText;

        void Start()
        {
            _errorText.text = $"Error: {GameState.Instance.Error}";
        }
    }
}