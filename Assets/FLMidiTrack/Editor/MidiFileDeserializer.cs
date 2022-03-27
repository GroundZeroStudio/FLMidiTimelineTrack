using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chasing.Midi.Timeline
{
    /// <summary>
    /// .midi�ļ������л���
    /// </summary>
    public class MidiFileDeserializer
    {
        private static MidiAnimationAsset ReadTrack(MidiDataStreamReader reader, uint tpqn)
        {
            // ���ͷ          MTrk��ASCII��
            string chunkIdent = reader.ReadChars(4);
            if (chunkIdent != "MTrk")
                throw new FormatException("�Ҳ������ٿ飺MTrk  �������ڷ����е�midi�ļ��Ƿ���ڹ��");

            // �������        ÿ�������ʼ�ı�־�� MTrk , ����� 4 �ֽھ��ǹ���ĳ��� ;
            uint chunkEnd = reader.ReadBEUInt32();
            chunkEnd += reader.Position;

            // MIDI event sequence  
            List<MidiEvent> events = new List<MidiEvent>();
            uint ticks = 0u;
            byte stat = 0;

            while (reader.Position < chunkEnd)
            {
                // Delta time   midi �е����� , �¼� ��ʱ���� , ����ͨ�� delta-time ���ֵ�
                ticks += reader.ReadMultiByteValue();

                // Status byte
                if ((reader.PeekByte() & 0x80u) != 0)
                    stat = reader.ReadByte();

                if (stat == 0xffu)
                {
                    // 0xff: Meta event (unused)
                    reader.Advance(1);
                    reader.Advance(reader.ReadMultiByteValue());
                }
                else if (stat == 0xf0u)
                {
                    // 0xf0: SysEx (unused)
                    while (reader.ReadByte() != 0xf7u) { }
                }
                else
                {
                    // MIDI event
                    var b1 = reader.ReadByte();
                    var b2 = (stat & 0xe0u) == 0xc0u ? (byte)0 : reader.ReadByte();
                    events.Add(new MidiEvent
                    {
                        time = ticks,
                        status = stat,
                        data1 = b1,
                        data2 = b2
                    });
                }
            }

            // Quantize duration with bars.
            uint bars = (ticks + tpqn * 4 - 1) / (tpqn * 4);

            // Asset instantiation
            MidiAnimationAsset asset = ScriptableObject.CreateInstance<MidiAnimationAsset>();
            asset.template.tempo = 120;
            asset.template.duration = bars * tpqn * 4;
            asset.template.ticksPerQuarterNote = tpqn;
            asset.template.events = events.ToArray();
            return asset;
        }


        #region Public

        public static MidiFileAsset Load(byte[] data)
        {
            var reader = new MidiDataStreamReader(data);

            // ͷ��ʶ        0 ~ 3 �ֽ� ,   " MThd " �ַ��� ASCII �� , ���� mid �ļ��ı�ʶ 
            string headerIdent = reader.ReadChars(4);
            if (headerIdent != "MThd")
                throw new FormatException("�Ҳ������ٿ飺MTrk  �������ڷ����е��ļ��Ƿ���.midi�ļ�");

            // ͷ����        4 ~ 7 �ֽ� , ���ݱ�ʾ mid �ļ��ļ�ͷ����
            uint headerLength = reader.ReadBEUInt32();
            if (headerLength != 6u)
                throw new FormatException("ͷ���ȱ����� 6");

            // MIDI�ļ�����  8 ~ 9 �ֽ� , ��ʾ mid �ļ��ĸ�ʽ
            // 0 : mid �ļ�ֻ��һ�����, ���е�ͨ������һ�������;
            // 1 : mid �ļ��ж������, ������ͬ����, �����еĹ��ͬʱ����;
            // 2 : mid �ļ��ж������, ��ͬ��;
            uint midiType = reader.ReadBEUInt16();
            //reader.Advance(2);

            // �������      10 ~ 11 �ֽ� , ��ʾ MIDI �������
            uint trackCount = reader.ReadBEUInt16();

            // ����ʱ��      12 ~ 13 �ֽ� , ����ָ������ʱ��
            uint tpqn = reader.ReadBEUInt16();
            if ((tpqn & 0x8000u) != 0)
                throw new FormatException("SMPTE time code is not supported.");

            // ���������ʼ
            MidiAnimationAsset[] tracks = new MidiAnimationAsset[trackCount];
            for (int i = 0; i < trackCount; i++)
                tracks[i] = ReadTrack(reader, tpqn);

            // MIDI��Դ����
            MidiFileAsset asset = ScriptableObject.CreateInstance<MidiFileAsset>();
            asset.tracks = tracks;
            return asset;
        }

        #endregion
    }

}
