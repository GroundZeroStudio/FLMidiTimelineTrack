using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chasing.Midi.Timeline
{
    /// <summary>
    /// Midi�ʲ� <ScriptableObject>
    /// 1. .midi�ļ���ת��Ϊ����Դ
    /// </summary>
    sealed public class MidiFileAsset : ScriptableObject
    {
        public MidiAnimationAsset[] tracks;
    }

}
