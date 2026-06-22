using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class UIEnterAndLeave : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI UIText;
    public CanvasGroup statusPanel;


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


            statusPanel.alpha = Mathf.Lerp(1, 0, t);
            if (t >= 1)
            {
                statusStarted = false;
            }

        }
    }

}
