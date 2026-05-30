using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Sizzle.AbilitySystem.Timeline
{
    [Serializable]
    public class AbilityTimelineCommandPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        [NoFoldOut]
        [NotKeyable]
        public AbilityTimelineCommandPlayableBehaviour template = new AbilityTimelineCommandPlayableBehaviour();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<AbilityTimelineCommandPlayableBehaviour>.Create(graph, template);
        }
    }
}