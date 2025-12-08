using TMPro;
using UnityEngine;

public class SentenceTaskUI : MonoBehaviour
{
    [SerializeField] private GameObject root;   // panel
    [SerializeField] private TMP_Text sentenceText;



    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();
    }

    public void ShowSentence(string sentence)
    {
        sentenceText.text = sentence;
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }


}
