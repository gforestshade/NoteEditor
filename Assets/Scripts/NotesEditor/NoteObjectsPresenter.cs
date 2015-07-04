﻿using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class NoteObjectsPresenter : MonoBehaviour
{
    [SerializeField]
    GameObject notesRegion;
    [SerializeField]
    CanvasEvents canvasEvents;
    [SerializeField]
    GameObject notePrefab;

    NotesEditorModel model;

    void Awake()
    {
        model = NotesEditorModel.Instance;


        var closestNoteAreaOnMouseDownObservable = canvasEvents.ScrollPadOnMouseDownObservable
            .Where(_ => 0 <= model.ClosestNotePosition.Value.samples);

        var closestNoteAreaOnMouseDownPosition = closestNoteAreaOnMouseDownObservable
            .Select(_ => Input.mousePosition)
            .ToReactiveProperty();

        var longNoteStartPosition = model.NormalNoteObservable
            .ToReactiveProperty();


        // Start editing of long note
        this.UpdateAsObservable()
            .SkipUntil(closestNoteAreaOnMouseDownObservable)
            .TakeWhile(_ => !Input.GetMouseButtonUp(0))
            .RepeatSafe()
            .Select(_ => Input.mousePosition)
            .Select(pos => (closestNoteAreaOnMouseDownPosition.Value - pos).magnitude)
            .Where(magnitude => 50 <= magnitude)
            .Select(_ => longNoteStartPosition.Value)
            .DistinctUntilChanged()
            .Do(_ => model.EditType.Value = NoteTypeEnum.LongNotes)
            .Subscribe(notePosition => model.LongNoteObservable.OnNext(notePosition));


        // Return to the normal notes edit mode
        this.UpdateAsObservable()
            .Where(_ => model.EditType.Value == NoteTypeEnum.LongNotes)
            .Where(_ => Input.GetKeyDown(KeyCode.Escape))
            .Subscribe(_ => model.EditType.Value = NoteTypeEnum.NormalNotes);

        var endLongNoteObservable = model.EditType.DistinctUntilChanged()
            .Where(editType => editType == NoteTypeEnum.NormalNotes)
            .Skip(1);

        model.AddedLongNoteObjectObservable.TakeUntil(endLongNoteObservable)
            .Buffer(2, 1).Where(b => 2 <= b.Count)
            .RepeatSafe()
            .Subscribe(b => {
                b[0].next = b[1];
                b[1].prev = b[0];
            });


        closestNoteAreaOnMouseDownObservable
            .Where(_ => model.EditType.Value == NoteTypeEnum.NormalNotes)
            .Subscribe(_ => model.NormalNoteObservable.OnNext(model.ClosestNotePosition.Value));

        closestNoteAreaOnMouseDownObservable
            .Where(_ => model.EditType.Value == NoteTypeEnum.LongNotes)
            .Subscribe(_ => model.LongNoteObservable.OnNext(model.ClosestNotePosition.Value));


        model.NormalNoteObservable.Subscribe(notePosition =>
        {
            if (model.NoteObjects.ContainsKey(notePosition))
            {
                RemoveNote(notePosition);
            }
            else
            {
                var noteObject = (Instantiate(notePrefab) as GameObject).GetComponent<NoteObject>();
                noteObject.notePosition = notePosition;
                noteObject.noteType.Value = NoteTypeEnum.NormalNotes;
                noteObject.transform.SetParent(notesRegion.transform);

                model.NoteObjects.Add(notePosition, noteObject);
            }
        });


        model.LongNoteObservable.Subscribe(notePosition => {
            if (model.NoteObjects.ContainsKey(notePosition))
            {
                var noteObject = model.NoteObjects[notePosition];

                if (noteObject.noteType.Value == NoteTypeEnum.LongNotes)
                {

                    if (noteObject.prev != null)
                        noteObject.prev.next = noteObject.next;

                    if (noteObject.next != null)
                        noteObject.next.prev = noteObject.prev;

                    RemoveNote(notePosition);
                }
                else
                {
                    noteObject.noteType.Value = NoteTypeEnum.LongNotes;
                    model.AddedLongNoteObjectObservable.OnNext(noteObject);
                }
            }
            else
            {
                var noteObject = (Instantiate(notePrefab) as GameObject).GetComponent<NoteObject>();
                noteObject.notePosition = notePosition;
                noteObject.noteType.Value = NoteTypeEnum.LongNotes;
                noteObject.transform.SetParent(notesRegion.transform);

                model.NoteObjects.Add(notePosition, noteObject);
                model.AddedLongNoteObjectObservable.OnNext(noteObject);
            }
        });
    }

    void RemoveNote(NotePosition notePosition)
    {
        if (model.NoteObjects.ContainsKey(notePosition))
        {
            var noteObject = model.NoteObjects[notePosition];
            model.NoteObjects.Remove(notePosition);
            DestroyObject(noteObject.gameObject);
        }
    }
}