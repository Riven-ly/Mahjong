using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GoldFlyControl : MonoBehaviour
{
    public Image icon;
    public AudioSource source;
    public ParticleSystem particleSystemObj;

    public ItemType itemType;

    private void OnEnable()
    {
        if (itemType == ItemType.Gold)
        {
            GameManager.Instance.UpdateAppATTToDiamond(icon);
        }
    }

    public void FlyGold(Vector3 start, Vector3 target,float awaitTime)
    {
        this.DOKill();
        gameObject.SetActive(true);
        //particleSystemObj.gameObject.SetActive(false);
        transform.localScale = Vector3.one;
        transform.position = start;
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-1f, 1f);
        Vector3 vec1 = start + new Vector3(randomX, randomY, 0);
        transform.DOMove(vec1, 0.3f).SetTarget(this);

        DOTween.Sequence().Append(transform.DOMove(vec1, 0.3f))
            .AppendInterval(awaitTime)
            .OnComplete(() =>
            {
                transform.DOScale(0.5f, 0.5f).SetTarget(this);
                transform.DOMove(target, 0.5f)
                .SetEase(Ease.OutCubic)//OutCubic
                .OnComplete(() =>
                {
                    AudioManager.Instance.SetAudioSource(source, "goldEffect");
                    icon.gameObject.SetActive(false);
                    //particleSystemObj.gameObject.SetActive(true);
                    //particleSystemObj.Play();
                    DOTween.Sequence().AppendInterval(0.5f).AppendCallback(() =>
                    {
                        icon.gameObject.SetActive(true);
                        gameObject.SetActive(false);
                    });
                })
                .SetTarget(this);
            })
            .SetTarget(this);
    }
}
