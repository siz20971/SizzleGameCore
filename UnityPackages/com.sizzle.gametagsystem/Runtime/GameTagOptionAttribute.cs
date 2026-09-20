using System;
using UnityEngine;

namespace Sizzle.GameTagSystem
{
    /// <summary>
    /// GameTag 필드에 부모(Parent) 태그를 지정하여, 인스펙터의 드롭다운 선택 메뉴를 해당 부모의 하위 태그로만 제한하고,
    /// 직접 텍스트를 입력할 때도 해당 부모 하위 태그만 입력되도록 제어하는 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GameTagOptionAttribute : PropertyAttribute
    {
        private string m_parent = string.Empty;

        /// <summary>
        /// 필터링 기준이 되는 부모 태그 경로입니다. (예: "Status.Buff")
        /// </summary>
        public string Parent
        {
            get => m_parent;
            set => m_parent = value ?? string.Empty;
        }

        /// <summary>
        /// 소문자 명명 파라미터 지원 프로퍼티 (예: [GameTagOption(parent="Status.Buff")])
        /// </summary>
        public string parent
        {
            get => Parent;
            set => Parent = value;
        }

        /// <summary>
        /// 부모 태그 자체(예: "Status.Buff")를 드롭다운 선택 항목 및 유효 입력값으로 포함할지 여부입니다.
        /// 기본값은 false(엄격한 하위 태그만 허용)입니다.
        /// </summary>
        public bool IncludeParent { get; set; } = false;

        /// <summary>
        /// 소문자 명명 파라미터 지원 프로퍼티
        /// </summary>
        public bool includeParent
        {
            get => IncludeParent;
            set => IncludeParent = value;
        }

        /// <summary>
        /// 드롭다운 메뉴에서 부모 경로 접두사를 생략하고 상대 경로로 표시할지 여부입니다.
        /// true일 경우 "Status.Buff.A"는 "A"로 깔끔하게 표시됩니다.
        /// 기본값은 true입니다.
        /// </summary>
        public bool RelativePathInMenu { get; set; } = true;

        /// <summary>
        /// 소문자 명명 파라미터 지원 프로퍼티
        /// </summary>
        public bool relativePathInMenu
        {
            get => RelativePathInMenu;
            set => RelativePathInMenu = value;
        }

        /// <summary>
        /// 직접 텍스트 입력 시 부모 하위 태그로 강제 제어할지 여부입니다.
        /// true일 경우 하위 태그명(예: "A")만 입력해도 자동으로 부모 접두사("Status.Buff.A")가 완성되며,
        /// 부모 범위를 벗어난 태그 입력은 차단 및 복원됩니다.
        /// 기본값은 true입니다.
        /// </summary>
        public bool RestrictToParent { get; set; } = true;

        /// <summary>
        /// 소문자 명명 파라미터 지원 프로퍼티
        /// </summary>
        public bool restrictToParent
        {
            get => RestrictToParent;
            set => RestrictToParent = value;
        }

        /// <summary>
        /// 빈 태그(빈 문자열) 입력을 허용할지 여부입니다. 기본값은 true입니다.
        /// </summary>
        public bool AllowEmpty { get; set; } = true;

        /// <summary>
        /// 소문자 명명 파라미터 지원 프로퍼티
        /// </summary>
        public bool allowEmpty
        {
            get => AllowEmpty;
            set => AllowEmpty = value;
        }

        /// <summary>
        /// 텍스트 직접 입력을 제한하고 오직 드롭다운 목록에서만 태그를 선택하도록 강제할지 여부입니다.
        /// 기본값은 false입니다.
        /// </summary>
        public bool DropdownOnly { get; set; } = false;

        /// <summary>
        /// 소문자 명명 파라미터 지원 프로퍼티
        /// </summary>
        public bool dropdownOnly
        {
            get => DropdownOnly;
            set => DropdownOnly = value;
        }

        /// <summary>
        /// 기본 생성자입니다.
        /// </summary>
        public GameTagOptionAttribute()
        {
            m_parent = string.Empty;
        }

        /// <summary>
        /// 부모 태그를 지정하는 생성자입니다. (예: [GameTagOption("Status.Buff")])
        /// </summary>
        /// <param name="parent">기준 부모 태그 경로</param>
        public GameTagOptionAttribute(string parent)
        {
            m_parent = parent ?? string.Empty;
        }
    }
}
