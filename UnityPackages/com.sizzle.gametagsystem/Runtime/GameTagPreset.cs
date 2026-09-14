using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.GameTagSystem
{
    /// <summary>
    /// 프로젝트 내에 생성해두면, 에디터에서 자동으로 수집되어 GameTagCache에 등록되는 프리셋 애셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameTagPreset", menuName = "Sizzle/Game Tag Preset")]
    public class GameTagPreset : ScriptableObject
    {
        [Tooltip("자동으로 수집되어 드롭다운 메뉴에 노출될 태그 목록입니다.")]
        [SerializeField] private List<GameTag> m_tags = new List<GameTag>();

        public IReadOnlyList<GameTag> Tags => m_tags;
    }
}
