using System;
using UnityEngine;

namespace GamePlay
{
    public enum StorySpeaker
    {
        [InspectorName("기본 / 이전 화자")] Inherit = 0,
        [InspectorName("무닌")] Munin = 1,
        [InspectorName("스벤")] Sven = 2,
    }

    public enum StoryExpression
    {
        [InspectorName("이전 표정 유지")] Inherit = 0,
        [InspectorName("기본")] Neutral = 1,
        [InspectorName("클로즈업")] CloseUp = 2,
        [InspectorName("기쁨")] Happy = 3,
        [InspectorName("놀람")] Surprised = 4,
        [InspectorName("슬픔")] Sad = 5,
        [InspectorName("화남")] Angry = 6,
    }

    [CreateAssetMenu(fileName = "NewSpeaker", menuName = "ProjectBB/Story Speaker")]
    public sealed class StorySpeakerProfile : ScriptableObject
    {
        [Serializable]
        private struct Portrait
        {
            [SerializeField] private StoryExpression _expression;
            [SerializeField] private Sprite _sprite;

            internal StoryExpression Expression => _expression;
            internal Sprite Sprite => _sprite;
        }

        [SerializeField] private StorySpeaker _speaker;
        [SerializeField] private string _nameKey;
        [SerializeField] private Sprite _neutralPortrait;
        [SerializeField] private Portrait[] _portraits;

        internal StorySpeaker Speaker => _speaker;
        internal string NameKey => _nameKey;

        public bool SupportsExpression(StorySpeaker speaker, StoryExpression expression)
        {
            return (speaker == StorySpeaker.Inherit || speaker == _speaker) && FindVariant(expression) != null;
        }

        internal Sprite GetPortrait(StoryExpression expression) => FindVariant(expression) ?? _neutralPortrait;

        private Sprite FindVariant(StoryExpression expression)
        {
            if (expression != StoryExpression.Inherit && expression != StoryExpression.Neutral && _portraits != null)
            {
                foreach (Portrait portrait in _portraits)
                {
                    if (portrait.Expression == expression && portrait.Sprite != null)
                    {
                        return portrait.Sprite;
                    }
                }
            }
            return null;
        }
    }
}
