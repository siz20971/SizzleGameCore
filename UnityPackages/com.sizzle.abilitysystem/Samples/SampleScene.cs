using UnityEngine;

namespace Sizzle.AbilitySystem.Samples
{
    public class SampleScene : MonoBehaviour
    {
        [SerializeField] private AbilityProcessor m_targetProcessor;

        private void Start()
        {
            m_targetProcessor.Initialize();
        }
    }
}
