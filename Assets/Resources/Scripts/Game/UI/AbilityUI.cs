using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AbilityUI : MonoBehaviour
{
    [SerializeField] private Image icon = default;
    [SerializeField] private Image coolDownImage = default;

    public void SetIcon(Sprite s)
    {
        icon.sprite = s;
    }
    public void ShowCoolDown(float cooldown)
    {
        transform.DOComplete();
        coolDownImage.fillAmount = 0;
        coolDownImage.DOFillAmount(1, cooldown).SetEase(Ease.Linear).OnComplete(() => transform.DOPunchScale(Vector3.one/10, .2f, 10, 1));
    }
}
