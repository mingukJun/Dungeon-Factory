using System;

public struct NodeAction
{
    public string label;        // 버튼에 표시될 텍스트 (또는 로컬라이즈 키)
    public Action callback;     // 버튼 눌렀을 때 실행할 행동

    public NodeAction(string label, Action callback)
    {
        this.label = label;
        this.callback = callback;
    }
}
