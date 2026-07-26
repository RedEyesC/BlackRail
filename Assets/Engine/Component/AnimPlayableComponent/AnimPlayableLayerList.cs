using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

public partial class AnimPlayableComponent
{
    internal sealed class AnimPlayableLayerList : IEnumerable<AnimPlayableLayer>
    {
        private const int DefaultCapacity = 4;
        private readonly AnimPlayableComponent owner;
        private AnimPlayableLayer[] layers = new AnimPlayableLayer[DefaultCapacity];
        private int count;

        public AnimPlayableLayerList(AnimPlayableComponent owner)
        {
            this.owner = owner;
        }

        public int Count => count;
        public bool IsValid => count > 0 && layers[0] != null && layers[0].Mixer.IsValid();

        public AnimPlayableLayer this[int index]
        {
            get
            {
                SetMinCount(index + 1);
                return layers[index];
            }
        }

        public AnimPlayableLayer GetLayer(int index)
        {
            if (index < 0 || index >= count)
            {
                return null;
            }

            return layers[index];
        }

        public void SetMinCount(int min)
        {
            if (min < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(min));
            }

            while (count < min)
            {
                Add();
            }
        }

        private AnimPlayableLayer Add()
        {
            int index = count;
            if (index >= layers.Length)
            {
                Array.Resize(ref layers, layers.Length * 2);
            }

            AnimPlayableLayer layer = new AnimPlayableLayer(owner, index);
            layer.SetMixer(AnimationMixerPlayable.Create(owner.graph, 0));

            count = index + 1;
            owner.layerMixer.SetInputCount(count);
            owner.graph.Connect(layer.Mixer, 0, owner.layerMixer, index);
            owner.layerMixer.SetInputWeight(index, index == 0 ? 1f : 0f);

            layers[index] = layer;
            return layer;
        }

        public void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                layers[i]?.Clear();
                layers[i] = null;
            }

            count = 0;
        }

        public IEnumerator<AnimPlayableLayer> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return layers[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
