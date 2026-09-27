using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sizzle.AbilitySystem.Actions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sizzle.AbilitySystem.Editor
{
    /// <summary>
    /// Sizzle.AbilitySystem의 ActionSequence 및 ActionCompositionAbility를
    /// 시각적인 노드 파이프라인 형태로 편집하는 전용 에디터 윈도우입니다.
    /// 단일 시퀀스뿐만 아니라, 하나의 어빌리티 내에 정의된 다중 분기 시퀀스(Branching Sequences)의 탭 전환 편집을 완벽히 지원합니다.
    /// </summary>
    public class ActionSequenceFlowWindow : EditorWindow
    {
        public struct SequenceInfo
        {
            public string DisplayName;
            public string PropertyPath;
            public int ActionCount;
        }

        private ScriptableObject m_targetAbility;
        private SerializedObject m_serializedObject;
        private SerializedProperty m_currentSequenceProperty; // 현재 선택된 List<AbilityAction> or List<CharacterAbilityActionBase>
        private string m_currentSequencePath;

        private List<SequenceInfo> m_availableSequences = new List<SequenceInfo>();
        private bool m_isLocked = false;
        private bool m_allExpanded = true;

        // 카드 패널 너비 설정
        private const int MIN_CARD_WIDTH = 280;
        private const int MAX_CARD_WIDTH = 750;
        private const string PREF_CARD_WIDTH = "Sizzle.SequenceFlow.CardWidth";
        private int m_cardWidth = MIN_CARD_WIDTH;

        // UI 요소 참조
        private VisualElement m_toolbarRoot;
        private ObjectField m_targetObjectField;
        private Button m_lockButton;
        private VisualElement m_sequenceSelectorContainer;
        private VisualElement m_contentContainer;
        private ScrollView m_flowScrollView;
        private VisualElement m_pipelineContainer;
        private VisualElement m_emptyOverlay;
        private Label m_actionCountLabel;

        [MenuItem("Sizzle/Ability System/Action Sequence Flow Editor", priority = 10)]
        [MenuItem("Tools/Sizzle/AbilitySystem/Action Sequence Flow Editor", priority = 20)]
        [MenuItem("Window/Ability System/Action Sequence Flow Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<ActionSequenceFlowWindow>("Sequence Flow Editor");
            window.minSize = new Vector2(680, 440);
            window.Show();
            window.TryLoadFromSelection();
        }

        public static void OpenWith(ScriptableObject target)
        {
            var window = GetWindow<ActionSequenceFlowWindow>("Sequence Flow Editor");
            window.minSize = new Vector2(680, 440);
            window.Show();
            window.SetTargetAbility(target);
        }

        public static void OpenSequence(ScriptableObject target, string sequencePropertyPath)
        {
            var window = GetWindow<ActionSequenceFlowWindow>("Sequence Flow Editor");
            window.minSize = new Vector2(680, 440);
            window.Show();
            window.SetTargetAbility(target, sequencePropertyPath);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Sequence Flow", EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image);
            m_cardWidth = EditorPrefs.GetInt(PREF_CARD_WIDTH, MIN_CARD_WIDTH);
            if (m_cardWidth < MIN_CARD_WIDTH) m_cardWidth = MIN_CARD_WIDTH;

            Selection.selectionChanged += OnSelectionChange;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);

            BuildToolbar();
            BuildMainCanvas();
            BuildEmptyOverlay();

            if (m_targetAbility == null)
            {
                TryLoadFromSelection();
            }
            else
            {
                BindTarget(m_targetAbility, m_currentSequencePath);
            }
        }

        #region Toolbar

        private void BuildToolbar()
        {
            m_toolbarRoot = new Toolbar();
            m_toolbarRoot.style.height = 32;
            m_toolbarRoot.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
            m_toolbarRoot.style.borderBottomWidth = 1;
            m_toolbarRoot.style.borderBottomColor = new Color(0.08f, 0.08f, 0.08f, 1f);
            m_toolbarRoot.style.alignItems = Align.Center;
            m_toolbarRoot.style.paddingLeft = 8;
            m_toolbarRoot.style.paddingRight = 8;

            // 라벨 & 타겟 필드
            var targetLabel = new Label("Target:")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 4, alignSelf = Align.Center, color = new Color(0.85f, 0.85f, 0.85f) }
            };
            m_toolbarRoot.Add(targetLabel);

            m_targetObjectField = new ObjectField
            {
                objectType = typeof(ScriptableObject),
                allowSceneObjects = false,
                value = m_targetAbility,
                style = { width = 180, alignSelf = Align.Center }
            };
            m_targetObjectField.RegisterValueChangedCallback(evt =>
            {
                var newTarget = evt.newValue as ScriptableObject;
                if (newTarget != m_targetAbility)
                {
                    if (newTarget != null && !IsValidTarget(newTarget))
                    {
                        EditorUtility.DisplayDialog("경고", "ActionSequence 또는 SequenceActions를 포함하는 에셋만 지정할 수 있습니다.", "확인");
                        m_targetObjectField.value = m_targetAbility;
                        return;
                    }
                    SetTargetAbility(newTarget);
                }
            });
            m_toolbarRoot.Add(m_targetObjectField);

            // Ping 버튼
            var pingButton = new ToolbarButton(() =>
            {
                if (m_targetAbility != null)
                {
                    EditorGUIUtility.PingObject(m_targetAbility);
                    Selection.activeObject = m_targetAbility;
                }
            })
            {
                text = "Ping",
                tooltip = "프로젝트 뷰에서 현재 에셋 강조"
            };
            m_toolbarRoot.Add(pingButton);

            // Lock 버튼
            m_lockButton = new ToolbarButton(() =>
            {
                m_isLocked = !m_isLocked;
                UpdateLockButtonState();
            })
            {
                tooltip = "선택 고정 (활성화 시 프로젝트 창에서 다른 에셋을 선택해도 현재 창이 바뀌지 않음)"
            };
            UpdateLockButtonState();
            m_toolbarRoot.Add(m_lockButton);

            // ── 다중 시퀀스 분기 선택 탭/드롭다운 영역 ──
            m_sequenceSelectorContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginLeft = 10,
                    marginRight = 10
                }
            };
            m_toolbarRoot.Add(m_sequenceSelectorContainer);

            // Spacer
            var spacer = new ToolbarSpacer();
            spacer.style.flexGrow = 1;
            m_toolbarRoot.Add(spacer);

            // 카드 패널 너비 조절 슬라이더 및 필드
            var widthContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 12 }
            };

            var widthLabel = new Label("Panel Width:")
            {
                style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 11, marginRight = 4 }
            };
            widthContainer.Add(widthLabel);

            var widthSlider = new SliderInt(MIN_CARD_WIDTH, MAX_CARD_WIDTH)
            {
                value = m_cardWidth,
                style = { width = 85, marginRight = 4 },
                tooltip = $"액션 카드 패널 가로 너비 조절 (최소: {MIN_CARD_WIDTH}px)"
            };

            var widthField = new IntegerField
            {
                value = m_cardWidth,
                style = { width = 45 },
                tooltip = $"액션 카드 패널 가로 너비 (최소: {MIN_CARD_WIDTH}px)"
            };

            widthSlider.RegisterValueChangedCallback(evt =>
            {
                widthField.SetValueWithoutNotify(evt.newValue);
                UpdateAllCardWidths(evt.newValue);
            });

            widthField.RegisterValueChangedCallback(evt =>
            {
                int clamped = Mathf.Clamp(evt.newValue, MIN_CARD_WIDTH, MAX_CARD_WIDTH);
                if (clamped != evt.newValue) widthField.SetValueWithoutNotify(clamped);
                widthSlider.SetValueWithoutNotify(clamped);
                UpdateAllCardWidths(clamped);
            });

            widthContainer.Add(widthSlider);
            widthContainer.Add(widthField);
            m_toolbarRoot.Add(widthContainer);

            // 액션 개수 표시
            m_actionCountLabel = new Label("Actions: 0")
            {
                style = { marginRight = 10, alignSelf = Align.Center, color = new Color(0.6f, 0.6f, 0.6f) }
            };
            m_toolbarRoot.Add(m_actionCountLabel);

            // 전체 접기/펼치기
            var expandToggleBtn = new ToolbarButton(() =>
            {
                m_allExpanded = !m_allExpanded;
                RefreshGraph();
            })
            {
                text = "Toggle Foldouts",
                tooltip = "모든 액션 카드의 세부 프로퍼티 접기/펼치기"
            };
            m_toolbarRoot.Add(expandToggleBtn);

            // + Add Action 메뉴 버튼
            var addActionBtn = new ToolbarButton(() =>
            {
                if (m_currentSequenceProperty == null) return;
                ShowAddActionMenu(m_currentSequenceProperty.arraySize);
            })
            {
                text = "＋ Add Action",
                tooltip = "현재 시퀀스 마지막에 새 액션 추가",
                style =
                {
                    backgroundColor = new Color(0.18f, 0.45f, 0.72f),
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            m_toolbarRoot.Add(addActionBtn);

            rootVisualElement.Add(m_toolbarRoot);
        }

        private void UpdateLockButtonState()
        {
            if (m_lockButton == null) return;
            m_lockButton.text = m_isLocked ? "🔒 Locked" : "🔓 Unlocked";
            m_lockButton.style.color = m_isLocked ? new Color(1f, 0.8f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
        }

        private void UpdateAllCardWidths(int newWidth)
        {
            m_cardWidth = Mathf.Clamp(newWidth, MIN_CARD_WIDTH, MAX_CARD_WIDTH);
            EditorPrefs.SetInt(PREF_CARD_WIDTH, m_cardWidth);

            if (m_pipelineContainer != null)
            {
                var cards = m_pipelineContainer.Query<VisualElement>(className: "action-card").ToList();
                foreach (var card in cards)
                {
                    card.style.width = m_cardWidth;
                }
            }
        }

        private void UpdateSequenceSelectorUI()
        {
            if (m_sequenceSelectorContainer == null) return;
            m_sequenceSelectorContainer.Clear();

            if (m_availableSequences.Count <= 1)
            {
                if (m_availableSequences.Count == 1)
                {
                    var singleLabel = new Label($"Sequence: {m_availableSequences[0].DisplayName}")
                    {
                        style = { color = new Color(0.8f, 0.85f, 0.95f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 }
                    };
                    m_sequenceSelectorContainer.Add(singleLabel);
                }
                return;
            }

            var prefix = new Label("Sequence:")
            {
                style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 11, marginRight = 4 }
            };
            m_sequenceSelectorContainer.Add(prefix);

            // 탭 버튼 또는 드롭다운으로 표시
            foreach (var seq in m_availableSequences)
            {
                bool isSelected = seq.PropertyPath == m_currentSequencePath;
                var tabBtn = new ToolbarButton(() =>
                {
                    SelectSequence(seq.PropertyPath);
                })
                {
                    text = $"{seq.DisplayName} ({seq.ActionCount})",
                    style =
                    {
                        backgroundColor = isSelected ? new Color(0.2f, 0.45f, 0.75f) : new Color(0.22f, 0.22f, 0.24f),
                        color = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.8f),
                        unityFontStyleAndWeight = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        borderBottomWidth = isSelected ? 2 : 0,
                        borderBottomColor = new Color(0.4f, 0.75f, 1f),
                        paddingLeft = 8,
                        paddingRight = 8,
                        marginRight = 2
                    }
                };
                m_sequenceSelectorContainer.Add(tabBtn);
            }
        }

        #endregion

        #region Canvas & Background

        private void BuildMainCanvas()
        {
            m_contentContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = new Color(0.13f, 0.13f, 0.14f, 1f)
                }
            };

            m_contentContainer.generateVisualContent += DrawBackgroundGrid;

            m_flowScrollView = new ScrollView(ScrollViewMode.Horizontal)
            {
                style =
                {
                    flexGrow = 1,
                    paddingTop = 40,
                    paddingBottom = 40,
                    paddingLeft = 30,
                    paddingRight = 60
                }
            };

            m_pipelineContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart
                }
            };

            m_flowScrollView.Add(m_pipelineContainer);
            m_contentContainer.Add(m_flowScrollView);
            rootVisualElement.Add(m_contentContainer);
        }

        private void DrawBackgroundGrid(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var width = m_contentContainer.resolvedStyle.width;
            var height = m_contentContainer.resolvedStyle.height;

            if (width <= 0 || height <= 0) return;

            painter.strokeColor = new Color(1f, 1f, 1f, 0.025f);
            painter.lineWidth = 1f;

            const float spacing = 32f;
            for (float x = 0; x < width; x += spacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }

            for (float y = 0; y < height; y += spacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
                painter.Stroke();
            }
        }

        #endregion

        #region Empty State

        private void BuildEmptyOverlay()
        {
            m_emptyOverlay = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0, top = 32, right = 0, bottom = 0,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f)
                }
            };

            var box = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    paddingTop = 24, paddingBottom = 24, paddingLeft = 32, paddingRight = 32,
                    backgroundColor = new Color(0.18f, 0.18f, 0.19f),
                    borderTopLeftRadius = 10, borderTopRightRadius = 10,
                    borderBottomLeftRadius = 10, borderBottomRightRadius = 10,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = new Color(0.28f, 0.28f, 0.3f),
                    borderBottomColor = new Color(0.28f, 0.28f, 0.3f),
                    borderLeftColor = new Color(0.28f, 0.28f, 0.3f),
                    borderRightColor = new Color(0.28f, 0.28f, 0.3f)
                }
            };

            var titleLabel = new Label("선택된 어빌리티 또는 시퀀스 에셋이 없습니다")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.9f, 0.9f, 0.9f),
                    marginBottom = 8
                }
            };
            box.Add(titleLabel);

            var descLabel = new Label("Project 창에서 ActionSequence를 포함하는 어빌리티(ActionCompositionAbility 등)를 선택하거나,\n상단 툴바에서 에셋을 지정하세요.")
            {
                style =
                {
                    fontSize = 12,
                    color = new Color(0.65f, 0.65f, 0.65f),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginBottom = 16
                }
            };
            box.Add(descLabel);

            var selectButton = new Button(ShowAbilityAssetPicker)
            {
                text = "프로젝트 내 시퀀스 가능 에셋 목록에서 선택",
                style =
                {
                    height = 30,
                    paddingLeft = 14, paddingRight = 14,
                    backgroundColor = new Color(0.22f, 0.45f, 0.72f),
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            box.Add(selectButton);

            m_emptyOverlay.Add(box);
            rootVisualElement.Add(m_emptyOverlay);
        }

        private void ShowAbilityAssetPicker()
        {
            var menu = new GenericMenu();
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            int foundCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && IsValidTarget(asset))
                {
                    foundCount++;
                    menu.AddItem(new GUIContent(asset.name), false, () => SetTargetAbility(asset));
                }
            }

            if (foundCount == 0)
            {
                menu.AddDisabledItem(new GUIContent("프로젝트 내에 시퀀스를 포함하는 에셋이 없습니다."));
            }

            menu.ShowAsContext();
        }

        #endregion

        #region Target & Sequence Binding

        private void OnSelectionChange()
        {
            if (m_isLocked) return;
            TryLoadFromSelection();
        }

        private void OnUndoRedo()
        {
            if (m_targetAbility != null && m_serializedObject != null)
            {
                m_serializedObject.Update();
                RefreshGraph();
            }
        }

        private void TryLoadFromSelection()
        {
            var activeObj = Selection.activeObject as ScriptableObject;
            if (activeObj != null && IsValidTarget(activeObj))
            {
                SetTargetAbility(activeObj);
            }
        }

        public void SetTargetAbility(ScriptableObject target, string preferredSequencePath = null)
        {
            m_targetAbility = target;
            if (m_targetObjectField != null)
            {
                m_targetObjectField.SetValueWithoutNotify(m_targetAbility);
            }
            BindTarget(target, preferredSequencePath);
        }

        private bool IsValidTarget(ScriptableObject obj)
        {
            if (obj == null) return false;
            var so = new SerializedObject(obj);
            return DiscoverSequences(so).Count > 0;
        }

        private List<SequenceInfo> DiscoverSequences(SerializedObject so)
        {
            var list = new List<SequenceInfo>();
            if (so == null) return list;

            var prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 1. ActionSequence 타입 필드 탐색
                if (prop.type == typeof(ActionSequence).Name || prop.type == "ActionSequence")
                {
                    var actions = prop.FindPropertyRelative("Actions");
                    list.Add(new SequenceInfo
                    {
                        DisplayName = prop.displayName,
                        PropertyPath = prop.propertyPath,
                        ActionCount = actions != null ? actions.arraySize : 0
                    });
                }
                // 2. NamedActionSequence 리스트 탐색
                else if (prop.isArray && prop.type.Contains("NamedActionSequence"))
                {
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var elem = prop.GetArrayElementAtIndex(i);
                        var nameProp = elem.FindPropertyRelative("Name");
                        var seqProp = elem.FindPropertyRelative("Sequence");
                        var actions = seqProp?.FindPropertyRelative("Actions");
                        string name = !string.IsNullOrEmpty(nameProp?.stringValue) ? nameProp.stringValue : $"Branch #{i + 1}";

                        list.Add(new SequenceInfo
                        {
                            DisplayName = name,
                            PropertyPath = seqProp?.propertyPath ?? elem.propertyPath,
                            ActionCount = actions != null ? actions.arraySize : 0
                        });
                    }
                }
                // 3. 기존 SequenceActions 직접 목록 호환 (ProjectParry 기존 어빌리티 지원)
                else if (prop.name == "SequenceActions" && prop.isArray)
                {
                    list.Add(new SequenceInfo
                    {
                        DisplayName = "Main Sequence",
                        PropertyPath = prop.propertyPath,
                        ActionCount = prop.arraySize
                    });
                }
            }

            return list;
        }

        private void BindTarget(ScriptableObject target, string preferredSequencePath = null)
        {
            if (target == null)
            {
                m_serializedObject = null;
                m_currentSequenceProperty = null;
                m_currentSequencePath = null;
                m_availableSequences.Clear();
                UpdateSequenceSelectorUI();
                if (m_emptyOverlay != null) m_emptyOverlay.style.display = DisplayStyle.Flex;
                if (m_actionCountLabel != null) m_actionCountLabel.text = "Actions: 0";
                m_pipelineContainer?.Clear();
                return;
            }

            m_serializedObject = new SerializedObject(target);
            m_availableSequences = DiscoverSequences(m_serializedObject);

            if (m_availableSequences.Count == 0)
            {
                m_emptyOverlay.style.display = DisplayStyle.Flex;
                return;
            }

            // 요청된 경로가 있거나 유효한 첫 번째 시퀀스 선택
            string targetPath = preferredSequencePath;
            if (string.IsNullOrEmpty(targetPath) || !m_availableSequences.Any(s => s.PropertyPath == targetPath))
            {
                targetPath = m_availableSequences[0].PropertyPath;
            }

            SelectSequence(targetPath);
        }

        public void SelectSequence(string sequencePropertyPath)
        {
            m_currentSequencePath = sequencePropertyPath;
            m_serializedObject.Update();

            var seqProp = m_serializedObject.FindProperty(m_currentSequencePath);
            if (seqProp == null)
            {
                m_emptyOverlay.style.display = DisplayStyle.Flex;
                return;
            }

            // ActionSequence 객체일 경우 내부의 Actions 배열 프로퍼티를 획득
            if (seqProp.type == typeof(ActionSequence).Name || seqProp.type == "ActionSequence")
            {
                m_currentSequenceProperty = seqProp.FindPropertyRelative("Actions");
            }
            else
            {
                m_currentSequenceProperty = seqProp;
            }

            m_emptyOverlay.style.display = DisplayStyle.None;
            UpdateSequenceSelectorUI();
            RefreshGraph();
        }

        #endregion

        #region Graph Generation

        private void RefreshGraph()
        {
            if (m_pipelineContainer == null || m_serializedObject == null || m_currentSequenceProperty == null)
                return;

            m_serializedObject.Update();
            m_pipelineContainer.Clear();

            int actionCount = m_currentSequenceProperty.arraySize;
            if (m_actionCountLabel != null)
            {
                m_actionCountLabel.text = $"Actions: {actionCount}";
            }

            // 1. [START] 노드 생성
            m_pipelineContainer.Add(CreateStartNode());

            // 2. 각 액션 카드와 연결선(Connector) 생성
            for (int i = 0; i < actionCount; i++)
            {
                int insertIndex = i;
                m_pipelineContainer.Add(CreateConnector(insertIndex));

                var elementProp = m_currentSequenceProperty.GetArrayElementAtIndex(i);
                m_pipelineContainer.Add(CreateActionCard(elementProp, i, actionCount));
            }

            // 3. 마지막 카드 -> [FINISH] 사이의 연결선
            m_pipelineContainer.Add(CreateConnector(actionCount));

            // 4. [FINISH] 노드 생성
            m_pipelineContainer.Add(CreateFinishNode());

            m_contentContainer?.MarkDirtyRepaint();
        }

        #endregion

        #region Node & Card Elements

        private VisualElement CreateStartNode()
        {
            var node = new VisualElement
            {
                style =
                {
                    width = 110,
                    height = 54,
                    alignSelf = Align.Center,
                    backgroundColor = new Color(0.15f, 0.45f, 0.25f),
                    borderTopLeftRadius = 27, borderTopRightRadius = 27,
                    borderBottomLeftRadius = 27, borderBottomRightRadius = 27,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = new Color(0.25f, 0.75f, 0.4f),
                    borderBottomColor = new Color(0.25f, 0.75f, 0.4f),
                    borderLeftColor = new Color(0.25f, 0.75f, 0.4f),
                    borderRightColor = new Color(0.25f, 0.75f, 0.4f),
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                },
                tooltip = "시퀀스 시작 (Start)"
            };

            var label = new Label("▶ START")
            {
                style =
                {
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13
                }
            };
            node.Add(label);
            return node;
        }

        private VisualElement CreateFinishNode()
        {
            var node = new VisualElement
            {
                style =
                {
                    width = 110,
                    height = 54,
                    alignSelf = Align.Center,
                    backgroundColor = new Color(0.38f, 0.18f, 0.45f),
                    borderTopLeftRadius = 27, borderTopRightRadius = 27,
                    borderBottomLeftRadius = 27, borderBottomRightRadius = 27,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = new Color(0.68f, 0.38f, 0.8f),
                    borderBottomColor = new Color(0.68f, 0.38f, 0.8f),
                    borderLeftColor = new Color(0.68f, 0.38f, 0.8f),
                    borderRightColor = new Color(0.68f, 0.38f, 0.8f),
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                },
                tooltip = "시퀀스 완료 (Complete)"
            };

            var label = new Label("■ COMPLETE")
            {
                style =
                {
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12
                }
            };
            node.Add(label);
            return node;
        }

        private VisualElement CreateConnector(int insertIndex)
        {
            var connector = new VisualElement
            {
                style =
                {
                    width = 54,
                    height = 100,
                    alignSelf = Align.Center,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    marginLeft = 2,
                    marginRight = 2
                }
            };

            connector.generateVisualContent += (mgc) =>
            {
                var painter = mgc.painter2D;
                float width = connector.resolvedStyle.width;
                float centerY = connector.resolvedStyle.height * 0.5f;

                if (width <= 0) return;

                painter.strokeColor = new Color(0.45f, 0.5f, 0.58f, 0.85f);
                painter.lineWidth = 2.5f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, centerY));
                painter.LineTo(new Vector2(width - 8, centerY));
                painter.Stroke();

                painter.fillColor = new Color(0.45f, 0.5f, 0.58f, 0.95f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(width, centerY));
                painter.LineTo(new Vector2(width - 9, centerY - 5.5f));
                painter.LineTo(new Vector2(width - 9, centerY + 5.5f));
                painter.ClosePath();
                painter.Fill();
            };

            var plusButton = new Button(() => ShowAddActionMenu(insertIndex))
            {
                text = "＋",
                tooltip = $"이 위치(인덱스 {insertIndex})에 새 액션 삽입",
                style =
                {
                    width = 22,
                    height = 22,
                    paddingTop = 0, paddingBottom = 0, paddingLeft = 0, paddingRight = 0,
                    borderTopLeftRadius = 11, borderTopRightRadius = 11,
                    borderBottomLeftRadius = 11, borderBottomRightRadius = 11,
                    backgroundColor = new Color(0.24f, 0.28f, 0.34f),
                    color = new Color(0.85f, 0.9f, 1f),
                    borderTopColor = new Color(0.4f, 0.45f, 0.55f),
                    borderBottomColor = new Color(0.4f, 0.45f, 0.55f),
                    borderLeftColor = new Color(0.4f, 0.45f, 0.55f),
                    borderRightColor = new Color(0.4f, 0.45f, 0.55f),
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13
                }
            };
            plusButton.RegisterCallback<MouseEnterEvent>(e => plusButton.style.backgroundColor = new Color(0.25f, 0.55f, 0.85f));
            plusButton.RegisterCallback<MouseLeaveEvent>(e => plusButton.style.backgroundColor = new Color(0.24f, 0.28f, 0.34f));

            connector.Add(plusButton);
            return connector;
        }

        private VisualElement CreateActionCard(SerializedProperty actionProp, int index, int totalCount)
        {
            var actionObj = actionProp.managedReferenceValue;
            Type actionType = actionObj?.GetType();
            string typeName = actionType != null ? FormatTypeName(actionType.Name) : "(Null Action)";
            Color accentColor = GetCategoryColor(actionType);

            var card = new VisualElement
            {
                style =
                {
                    width = m_cardWidth,
                    minWidth = MIN_CARD_WIDTH,
                    backgroundColor = new Color(0.18f, 0.18f, 0.19f),
                    borderTopLeftRadius = 8, borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8, borderBottomRightRadius = 8,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = new Color(0.28f, 0.28f, 0.3f),
                    borderBottomColor = new Color(0.28f, 0.28f, 0.3f),
                    borderLeftColor = new Color(0.28f, 0.28f, 0.3f),
                    borderRightColor = new Color(0.28f, 0.28f, 0.3f),
                    overflow = Overflow.Hidden,
                    marginBottom = 10
                }
            };
            card.AddToClassList("action-card");

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    backgroundColor = accentColor,
                    paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 6
                },
                tooltip = actionType != null ? $"{actionType.FullName}\n(더블 클릭 시 C# 소스 파일 열기)" : "Null"
            };

            if (actionType != null)
            {
                header.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.clickCount == 2)
                    {
                        OpenActionScript(actionType);
                        evt.StopPropagation();
                    }
                });
            }

            var headerLeft = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, overflow = Overflow.Hidden }
            };

            var badge = new Label($"#{index + 1}")
            {
                style =
                {
                    backgroundColor = new Color(0f, 0f, 0f, 0.35f),
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 11,
                    paddingTop = 2, paddingBottom = 2, paddingLeft = 5, paddingRight = 5,
                    marginRight = 6,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4, borderBottomRightRadius = 4
                }
            };
            headerLeft.Add(badge);

            var titleLabel = new Label(typeName)
            {
                style =
                {
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    textOverflow = TextOverflow.Ellipsis
                },
                tooltip = actionType != null ? $"{actionType.FullName}\n(더블 클릭 시 C# 소스 파일 열기)" : "Null"
            };
            headerLeft.Add(titleLabel);
            header.Add(headerLeft);

            var headerRight = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
            };

            var leftBtn = CreateSmallHeaderButton("◀", "앞으로 순서 이동", () =>
            {
                if (index > 0) MoveAction(index, index - 1);
            });
            leftBtn.SetEnabled(index > 0);
            headerRight.Add(leftBtn);

            var rightBtn = CreateSmallHeaderButton("▶", "뒤로 순서 이동", () =>
            {
                if (index < totalCount - 1) MoveAction(index, index + 1);
            });
            rightBtn.SetEnabled(index < totalCount - 1);
            headerRight.Add(rightBtn);

            var dupBtn = CreateSmallHeaderButton("⧉", "액션 복제 (하위 참조 깊은 복사)", () => DuplicateAction(index));
            headerRight.Add(dupBtn);

            if (actionType != null)
            {
                var scriptBtn = CreateSmallHeaderButton("CS", "해당 액션 C# 스크립트 열기 (더블 클릭으로도 가능)", () => OpenActionScript(actionType));
                scriptBtn.style.color = new Color(0.4f, 0.9f, 1f);
                scriptBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                scriptBtn.style.fontSize = 10;
                headerRight.Add(scriptBtn);
            }

            var delBtn = CreateSmallHeaderButton("✕", "액션 삭제", () => DeleteAction(index));
            delBtn.style.color = new Color(1f, 0.4f, 0.4f);
            headerRight.Add(delBtn);

            header.Add(headerRight);
            card.Add(header);

            // ── 본문 (Body: 프로퍼티 인스펙터) ──
            var body = new VisualElement
            {
                style =
                {
                    paddingTop = 8, paddingBottom = 8, paddingLeft = 10, paddingRight = 10,
                    backgroundColor = new Color(0.15f, 0.15f, 0.16f)
                }
            };

            var foldout = new Foldout
            {
                text = "Properties",
                value = m_allExpanded,
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 }
            };

            var propertyContainer = new VisualElement
            {
                style = { paddingTop = 4 }
            };

            bool hasProperties = RenderActionProperties(actionProp, propertyContainer);
            if (!hasProperties)
            {
                var emptyLabel = new Label("설정 가능한 프로퍼티가 없습니다.")
                {
                    style = { color = new Color(0.55f, 0.55f, 0.55f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Italic }
                };
                propertyContainer.Add(emptyLabel);
            }

            foldout.Add(propertyContainer);
            body.Add(foldout);
            card.Add(body);

            return card;
        }

        private Button CreateSmallHeaderButton(string text, string tooltip, Action onClick)
        {
            var btn = new Button(onClick)
            {
                text = text,
                tooltip = tooltip,
                style =
                {
                    width = 20,
                    height = 20,
                    paddingTop = 0, paddingBottom = 0, paddingLeft = 0, paddingRight = 0,
                    marginLeft = 2,
                    fontSize = 11,
                    backgroundColor = new Color(0f, 0f, 0f, 0.25f),
                    color = new Color(0.9f, 0.9f, 0.9f),
                    borderTopWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderRightWidth = 0,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3
                }
            };
            btn.RegisterCallback<MouseEnterEvent>(e => btn.style.backgroundColor = new Color(1f, 1f, 1f, 0.2f));
            btn.RegisterCallback<MouseLeaveEvent>(e => btn.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f));
            return btn;
        }

        private bool RenderActionProperties(SerializedProperty actionProp, VisualElement container)
        {
            var child = actionProp.Copy();
            var end = actionProp.GetEndProperty();
            bool enterChildren = true;
            bool foundAny = false;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                foundAny = true;

                var propField = new PropertyField(child.Copy());
                propField.Bind(m_serializedObject);
                propField.style.marginBottom = 3;
                container.Add(propField);
            }

            return foundAny;
        }

        #endregion

        #region Action Operations (Add, Move, Duplicate, Delete)

        private void ShowAddActionMenu(int insertIndex)
        {
            var menu = new GenericMenu();
            var candidateTypes = new HashSet<Type>();

            // Sizzle 코어의 AbilityAction 파생 타입 탐색
            foreach (var t in TypeCache.GetTypesDerivedFrom<AbilityAction>())
            {
                if (!t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                {
                    candidateTypes.Add(t);
                }
            }

            // 호환성: 프로젝트 레이어의 이전 CharacterAbilityActionBase 파생 타입 탐색
            var legacyBaseType = Type.GetType("ProjectP.Abilities.Actions.CharacterAbilityActionBase, Assembly-CSharp");
            if (legacyBaseType != null)
            {
                foreach (var t in TypeCache.GetTypesDerivedFrom(legacyBaseType))
                {
                    if (!t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                    {
                        candidateTypes.Add(t);
                    }
                }
            }

            var sortedTypes = candidateTypes.OrderBy(t => t.Name).ToList();

            if (sortedTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("사용 가능한 액션 타입이 없습니다."));
            }
            else
            {
                foreach (var type in sortedTypes)
                {
                    string category = GetTypeCategory(type);
                    string displayName = FormatTypeName(type.Name);
                    string menuPath = $"{category}/{displayName}";

                    menu.AddItem(new GUIContent(menuPath), false, () => InsertAction(type, insertIndex));
                }
            }

            menu.ShowAsContext();
        }

        private void InsertAction(Type actionType, int index)
        {
            if (m_serializedObject == null || m_currentSequenceProperty == null) return;

            m_serializedObject.Update();
            object instance = Activator.CreateInstance(actionType);

            m_currentSequenceProperty.InsertArrayElementAtIndex(index);
            var element = m_currentSequenceProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = instance;

            m_serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_targetAbility);
            RefreshGraph();
        }

        private void MoveAction(int fromIndex, int toIndex)
        {
            if (m_serializedObject == null || m_currentSequenceProperty == null) return;

            m_serializedObject.Update();
            m_currentSequenceProperty.MoveArrayElement(fromIndex, toIndex);
            m_serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_targetAbility);
            RefreshGraph();
        }

        private void DuplicateAction(int index)
        {
            if (m_serializedObject == null || m_currentSequenceProperty == null) return;

            m_serializedObject.Update();
            object originalAction = m_currentSequenceProperty.GetArrayElementAtIndex(index).managedReferenceValue;
            if (originalAction == null) return;

            object clonedAction = null;
            if (originalAction is AbilityAction abilityAction)
            {
                clonedAction = abilityAction.Clone();
            }
            else
            {
                // Reflection fallback for Clone() method
                var cloneMethod = originalAction.GetType().GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance);
                if (cloneMethod != null)
                {
                    clonedAction = cloneMethod.Invoke(originalAction, null);
                }
            }

            if (clonedAction == null) return;

            int targetIndex = index + 1;
            m_currentSequenceProperty.InsertArrayElementAtIndex(targetIndex);
            var newElem = m_currentSequenceProperty.GetArrayElementAtIndex(targetIndex);
            newElem.managedReferenceValue = clonedAction;

            m_serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_targetAbility);
            RefreshGraph();
        }

        private void DeleteAction(int index)
        {
            if (m_serializedObject == null || m_currentSequenceProperty == null) return;

            if (EditorUtility.DisplayDialog("액션 삭제", $"#{index + 1} 액션을 시퀀스에서 제거하시겠습니까?", "삭제", "취소"))
            {
                m_serializedObject.Update();
                int prevSize = m_currentSequenceProperty.arraySize;
                m_currentSequenceProperty.DeleteArrayElementAtIndex(index);
                if (m_currentSequenceProperty.arraySize == prevSize)
                {
                    m_currentSequenceProperty.DeleteArrayElementAtIndex(index);
                }
                m_serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_targetAbility);
                RefreshGraph();
            }
        }

        #endregion

        #region Utility & Styling Helpers

        private string GetTypeCategory(Type type)
        {
            if (type.Namespace != null && type.Namespace.StartsWith("Sizzle.AbilitySystem"))
                return "1. Sizzle Core";
            if (type.Name.Contains("Movement"))
                return "2. Movement";
            if (type.Name.Contains("Attack") || type.Name.Contains("Projectile") || type.Name.Contains("Combat"))
                return "3. Combat & Attack";
            if (type.Name.Contains("Wait") || type.Name.Contains("Delay") || type.Name.Contains("Timer"))
                return "4. Timing & Delay";
            if (type.Name.Contains("Facing") || type.Name.Contains("Rotate") || type.Name.Contains("Direction"))
                return "5. Orientation & Facing";
            if (type.Name.Contains("Debug") || type.Name.Contains("Log"))
                return "6. Debug & Testing";

            return "7. Custom";
        }

        private Color GetCategoryColor(Type type)
        {
            if (type == null) return new Color(0.3f, 0.3f, 0.3f);

            if (type.Namespace != null && type.Namespace.StartsWith("Sizzle.AbilitySystem"))
                return new Color(0.18f, 0.42f, 0.58f); // Sizzle Blue
            if (type.Name.Contains("Movement"))
                return new Color(0.16f, 0.42f, 0.65f); // Dodger Blue
            if (type.Name.Contains("Attack") || type.Name.Contains("Projectile") || type.Name.Contains("Combat"))
                return new Color(0.72f, 0.22f, 0.22f); // Coral Red
            if (type.Name.Contains("Wait") || type.Name.Contains("Delay") || type.Name.Contains("Timer"))
                return new Color(0.48f, 0.22f, 0.6f); // Amethyst Purple
            if (type.Name.Contains("Facing") || type.Name.Contains("Rotate") || type.Name.Contains("Direction"))
                return new Color(0.12f, 0.52f, 0.45f); // Teal
            if (type.Name.Contains("Debug") || type.Name.Contains("Log"))
                return new Color(0.75f, 0.45f, 0.12f); // Amber

            return new Color(0.24f, 0.28f, 0.34f); // Slate Gray
        }

        private string FormatTypeName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Empty Action";
            if (rawName == "Action") return "Action";
            if (rawName.EndsWith("Action") && rawName.Length > "Action".Length)
            {
                rawName = rawName.Substring(0, rawName.Length - "Action".Length);
            }

            return System.Text.RegularExpressions.Regex.Replace(rawName, "(\\B[A-Z])", " $1") + " Action";
        }

        private void OpenActionScript(Type actionType)
        {
            if (actionType == null) return;

            string typeName = actionType.Name;
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeName}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs")) continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;

                if (script.GetClass() == actionType)
                {
                    AssetDatabase.OpenAsset(script);
                    return;
                }

                if (System.IO.File.Exists(path))
                {
                    string[] lines = System.IO.File.ReadAllLines(path);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains($"class {typeName}"))
                        {
                            AssetDatabase.OpenAsset(script, i + 1);
                            return;
                        }
                    }
                }
            }

            // 전체 스크립트 검색 (Packages 및 Assets 포함)
            string[] allScriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (string guid in allScriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs") || !System.IO.File.Exists(path)) continue;

                string[] lines = System.IO.File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains($"class {typeName}"))
                    {
                        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                        if (script != null)
                        {
                            AssetDatabase.OpenAsset(script, i + 1);
                            return;
                        }
                    }
                }
            }

            EditorUtility.DisplayDialog("스크립트 열기 실패", $"{typeName}의 소스 코드 파일을 찾을 수 없습니다.", "확인");
        }

        #endregion
    }
}
