// ------------------------------------------------------------------------------
// 이 파일은 Ability 애셋들에 직접 작성된 태그가 아닌 커스텀 태그를 정의하는 파일입니다.
// 프로젝트에 이 파일이 없을 경우에만, 빈 클래스로 파일이 추가됩니다.
//
// * 자유롭게 태그를 추가해주세요.
// * 경로를 변경할 경우, GameTag의 선택 리스트에서 정의된 태그들이 보여지지 않습니다.
// * public static readonly GameTag [TAG_NAME] = "TagValue" 형식을 읽어들입니다.
//
// 이 코드는 'AbilityGameTagCodeGenerator.cs'에 의해 최초 생성되었습니다.
// ------------------------------------------------------------------------------
using Sizzle.GameTagSystem;

public static partial class AbilityTags
{
    public static readonly GameTag ABILITY_CHARACTER_COMMON_BASIC = new GameTag("Ability.Character.Common.Basic");
    public static readonly GameTag ABILITY_CHARACTER_COMMON_SPECIAL = new GameTag("Ability.Character.Common.Special");
    public static readonly GameTag ABILITY_CHARACTER_COMMON_GUARD = new GameTag("Ability.Character.Common.Guard");
    public static readonly GameTag ABILITY_CHARACTER_PLAYER_BASIC = new GameTag("Ability.Character.Player.Basic");
    public static readonly GameTag ABILITY_CHARACTER_PLAYER_SPECIAL = new GameTag("Ability.Character.Player.Special");
    public static readonly GameTag ABILITY_CHARACTER_PLAYER_GUARD = new GameTag("Ability.Character.Player.Guard");

    public static readonly GameTag STATE_COMMON_BASIC = new GameTag("State.Common.Basic");
    public static readonly GameTag STATE_COMMON_SPECIAL = new GameTag("State.Common.Special");
    public static readonly GameTag STATE_COMMON_GUARD = new GameTag("State.Common.Guard");

    public static readonly GameTag STATE_PLAYER_BASIC = new GameTag("State.Player.Basic");
    public static readonly GameTag STATE_PLAYER_SPECIAL = new GameTag("State.Player.Special");
    public static readonly GameTag STATE_PLAYER_GUARD = new GameTag("State.Player.Guard");
}
