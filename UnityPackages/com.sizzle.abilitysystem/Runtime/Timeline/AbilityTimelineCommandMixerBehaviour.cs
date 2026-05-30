using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Sizzle.AbilitySystem.Timeline
{
    public class AbilityTimelineCommandMixerBehaviour : PlayableBehaviour
    {
        private bool[] m_triggeredInputs = Array.Empty<bool>();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            int inputCount = playable.GetInputCount();
            EnsureCapacity(inputCount);

            GameObject boundObject = playerData as GameObject;

            for (int i = 0; i < inputCount; i++)
            {
                bool isActive = playable.GetInputWeight(i) > 0f;
                if (!isActive)
                {
                    m_triggeredInputs[i] = false;
                    continue;
                }

                if (m_triggeredInputs[i])
                    continue;

                ScriptPlayable<AbilityTimelineCommandPlayableBehaviour> inputPlayable =
                    (ScriptPlayable<AbilityTimelineCommandPlayableBehaviour>)playable.GetInput(i);
                AbilityTimelineCommandPlayableBehaviour input = inputPlayable.GetBehaviour();
                input.Execute(boundObject);
                m_triggeredInputs[i] = true;
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            m_triggeredInputs = Array.Empty<bool>();
        }

        private void EnsureCapacity(int inputCount)
        {
            if (m_triggeredInputs.Length == inputCount)
                return;

            m_triggeredInputs = new bool[inputCount];
        }
    }
}