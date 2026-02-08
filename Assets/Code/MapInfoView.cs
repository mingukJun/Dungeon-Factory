using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif
using Sirenix.OdinInspector;
using UnityEngine.UI;

public class MapInfoView : MonoBehaviour
{
#if TMP_PRESENT
    [Required] public TextMeshProUGUI statAtk;
    [Required] public TextMeshProUGUI statDef;
    [Required] public TextMeshProUGUI statHp;
    [Required] public TextMeshProUGUI deathProbText;
#else
    [Required] public Text statAtk, statDef, statHp, deathProbText;
#endif
    [Required] public Image deathProbBar; // fillAmount 사용

    MapPopupProfile _profile;

    public void Setup(MapPopupProfile profile, MapPopupData data)
    {
        _profile = profile;
        statAtk.text = data.recommended.atk.ToString();
        statDef.text = data.recommended.def.ToString();
        statHp.text = data.recommended.hp.ToString();

        float p = Mathf.Clamp01(data.partyDeathProbability);
        deathProbBar.fillAmount = p;
        deathProbText.text = $"{Mathf.RoundToInt(p * 100f)}%";

        // 경고색 등은 UI쪽에서 ColorBlock으로 처리하거나, 여기서 색 스왑
        if (p >= _profile.deathProbWarnThreshold)
            deathProbText.color = new Color(1f, 0.35f, 0.35f);
        else
            deathProbText.color = Color.white;
    }
}
