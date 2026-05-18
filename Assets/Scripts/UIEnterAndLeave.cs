using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class UIEnterAndLeave : MonoBehaviourPunCallbacks
{
#if UNITY_WSA
    public TextMeshPro UIText;
    public GameObject statusPanel;
#else
    public TextMeshProUGUI UIText;
    public CanvasGroup statusPanel;
#endif

    float duration = 5.0f;
    bool statusStarted;
    float t;

    private void Start()
    {
        statusStarted = false;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UIText.text = newPlayer.NickName + " has entered the room";
        t = 0;
        statusStarted = true;

        //if (this.JoinClip != null)
        //{
        //    if (this.source == null) this.source = FindObjectOfType<AudioSource>();
        //    this.source.PlayOneShot(this.JoinClip);
        //}
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UIText.text = otherPlayer.NickName + " has left the room";
        t = 0;
        statusStarted = true;


        //if (this.LeaveClip != null)
        //{
        //    if (this.source == null) this.source = FindObjectOfType<AudioSource>();
        //    this.source.PlayOneShot(this.LeaveClip);
        //}
    }

    private void Update()
    {
        if (statusStarted)
        {
            t += Time.deltaTime / duration ;


#if UNITY_WSA
            UIText.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, t));
            if (t >= 1)
            {
                statusStarted = false;
            }

#else
            statusPanel.alpha = Mathf.Lerp(1, 0, t);
            if (t >= 1)
            {
                statusStarted = false;
            }
#endif
        }
    }

}
