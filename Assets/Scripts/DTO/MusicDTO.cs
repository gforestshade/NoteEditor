﻿using System.Collections.Generic;

namespace NoteEditor.DTO
{
    public class MusicDTO
    {
        [System.Serializable]
        public class EditData
        {
            public string name;
            public int maxBlock;
            public float BPM;
            public int offset;
            public List<Note> notes;
        }

        [System.Serializable]
        public class Note
        {
            public int LPB;
            public int num;
            public int block;
            public int type;
            public int attributes;
            public int direction;
            public List<Note> notes;
        }
    }
}
