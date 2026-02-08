using UnityEngine;
using System;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public enum PartyRole { Warrior, Dealer }

[CreateAssetMenu(menuName = "Balance/DeathProbConfig", fileName = "DeathProbConfig")]
public class DeathProbConfig : ScriptableObject
{
    // --------------------------------------------------------
    // 1. 곡선 파라미터
    // --------------------------------------------------------
#if UNITY_EDITOR
    [TitleGroup("Defense Curve")]
    [Tooltip("방어력 체감 상수 (EffDef = Kdef일 때 감쇠 ~50%)")]
    [MinValue(1), LabelText("Kdef")]
#endif
    public float Kdef = 600f;

#if UNITY_EDITOR
    [TitleGroup("Logistic")]
    [Tooltip("로지스틱 경사 (크면 L=1 경계에서 전이가 날카로움)")]
    [Range(1f, 16f), LabelText("Steep")]
#endif
    public float steep = 8f;

#if UNITY_EDITOR
    [TitleGroup("Stability")]
    [Range(0f, 0.1f), LabelText("pMin")]
#endif
    public float pMin = 0.001f;

#if UNITY_EDITOR
    [Range(0.9f, 1f), LabelText("pMax")]
#endif
    public float pMax = 0.999f;

    // --------------------------------------------------------
    // 2. 던전 / 파티 공통 파라미터 (패턴 A)
    // --------------------------------------------------------
#if UNITY_EDITOR
    [TitleGroup("Dungeon")] // 부모 그룹 먼저 선언
    [TitleGroup("Dungeon/Party")]
    [HorizontalGroup("Dungeon/Party/S1"), LabelText("Dungeon ATK"), MinValue(0)]
#endif
    public float dungeonATK = 1200f;

#if UNITY_EDITOR
    [HorizontalGroup("Dungeon/Party/S1"), LabelText("Warrior DEF"), MinValue(0)]
#endif
    public float warriorDEF = 800f;

#if UNITY_EDITOR
    [HorizontalGroup("Dungeon/Party/S1"), LabelText("Dealer Count"), MinValue(1)]
#endif
    public int dealerCount = 3;

    // --------------------------------------------------------
    // 3. 파티 구성 테이블
    // --------------------------------------------------------
    [Serializable]
    public class MemberCase
    {
#if UNITY_EDITOR
        [HorizontalGroup("row", width: 90), LabelText("Role")]
#endif
        public PartyRole role = PartyRole.Dealer;

#if UNITY_EDITOR
        [HorizontalGroup("row"), LabelText("HP"), MinValue(1)]
#endif
        public float hp = 1000f;

#if UNITY_EDITOR
        [HorizontalGroup("row"), LabelText("Evasion"), Range(0, 1)]
#endif
        public float evasion = 0.15f;

#if UNITY_EDITOR
        [HorizontalGroup("row"), LabelText("λ (평균 시도수)"), MinValue(0)]
#endif
        public float lambda = 18f;

        // 계산 결과 미리보기
#if UNITY_EDITOR
        [ShowInInspector, ReadOnly, HorizontalGroup("row"), LabelText("q (per-hit)")]
#endif
        public float q_preview;

#if UNITY_EDITOR
        [ShowInInspector, ReadOnly, HorizontalGroup("row"), LabelText("P_run")]
#endif
        public float P_run_preview;
    }

#if UNITY_EDITOR
    [TitleGroup("Preview (Party Members)")]
    [TableList(AlwaysExpanded = true)]
#endif
    public MemberCase[] party = new MemberCase[]
    {
        new MemberCase { role = PartyRole.Warrior, hp = 1200, evasion = 0.10f, lambda = 35f },
        new MemberCase { role = PartyRole.Dealer,  hp = 1000, evasion = 0.20f, lambda = 18f },
        new MemberCase { role = PartyRole.Dealer,  hp = 1000, evasion = 0.20f, lambda = 18f },
        new MemberCase { role = PartyRole.Dealer,  hp = 1000, evasion = 0.20f, lambda = 18f },
    };

    // --------------------------------------------------------
    // 4. 수학 모델
    // --------------------------------------------------------
    public float GetEffectiveDefense(PartyRole role, float warriorDEF, int dealerCount)
    {
        if (role == PartyRole.Warrior) return Mathf.Max(0f, warriorDEF);
        int d = Mathf.Max(1, dealerCount);
        return Mathf.Max(0f, warriorDEF / d);
    }

    public float PerHitInstantDeathProb(float dungeonATK, float hp, float effDef, float evasion)
    {
        dungeonATK = Mathf.Max(0f, dungeonATK);
        hp = Mathf.Max(1f, hp);
        evasion = Mathf.Clamp01(evasion);
        float K = Mathf.Max(1f, Kdef);

        float mitigation = effDef / (effDef + K);
        float dmg = dungeonATK * (1f - mitigation);
        float L = dmg / hp;

        float x = steep * (L - 1f);
        float pKillOnHit = 1f / (1f + Mathf.Exp(-x));
        float q = (1f - evasion) * Mathf.Clamp(pKillOnHit, pMin, pMax);
        return Mathf.Clamp01(q);
    }

    public float RunDeathProb_Poisson(float q, float lambda)
    {
        q = Mathf.Clamp01(q);
        lambda = Mathf.Max(0f, lambda);
        return Mathf.Clamp01(1f - Mathf.Exp(-lambda * q));
    }

    public float RunDeathProb_FixedH(float q, int H)
    {
        H = Mathf.Max(0, H);
        if (H == 0) return 0f;
        q = Mathf.Clamp01(q);
        return Mathf.Clamp01(1f - Mathf.Pow(1f - q, H));
    }

    // --------------------------------------------------------
    // 5. 오딘용 버튼 및 미리보기
    // --------------------------------------------------------
#if UNITY_EDITOR
    [TitleGroup("Preview (Party Members)")]
    [Button("Recalculate Preview"), GUIColor(0.3f, 0.8f, 1f)]
    public void RecalcPreview()
    {
        if (party == null) return;
        foreach (var m in party)
        {
            float effDef = GetEffectiveDefense(m.role, warriorDEF, dealerCount);
            m.q_preview = PerHitInstantDeathProb(dungeonATK, m.hp, effDef, m.evasion);
            m.P_run_preview = RunDeathProb_Poisson(m.q_preview, m.lambda);
        }
    }

    [ShowInInspector, ReadOnly, TitleGroup("Preview (Party Members)"), LabelText("Σ P_run (기대 사망자 수)")]
    public float ExpectedDeathsSum
    {
        get
        {
            float sum = 0f;
            if (party != null)
            {
                foreach (var m in party)
                    sum += m.P_run_preview;
            }
            return sum;
        }
    }

    [TitleGroup("Preview (Party Members)")]
    [Button("Reset Party to 1W+3D"), GUIColor(0.9f, 0.9f, 0.2f)]
    public void ResetParty()
    {
        party = new MemberCase[]
        {
            new MemberCase { role = PartyRole.Warrior, hp = 1200, evasion = 0.10f, lambda = 35f },
            new MemberCase { role = PartyRole.Dealer,  hp = 1000, evasion = 0.20f, lambda = 18f },
            new MemberCase { role = PartyRole.Dealer,  hp = 1000, evasion = 0.20f, lambda = 18f },
            new MemberCase { role = PartyRole.Dealer,  hp = 1000, evasion = 0.20f, lambda = 18f },
        };
        RecalcPreview();
    }
#endif
}
