using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Sizzle.AbilitySystem.Timeline
{
    [TrackColor(0.23f, 0.48f, 0.20f)]
    [TrackClipType(typeof(AbilityTimelineCommandPlayableAsset))]
    [TrackBindingType(typeof(GameObject))]
    public class AbilityTimelineCommandTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<AbilityTimelineCommandMixerBehaviour>.Create(graph, inputCount);
        }

        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "Ability Command";
            base.OnCreateClip(clip);
        }
    }
}